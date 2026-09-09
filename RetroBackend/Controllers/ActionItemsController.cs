using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RetroBackend.Dtos;
using RetroBackend.Mappings;
using RetroBackend.Auth;
using RetroBackend.Services;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/actionitems")]
[Authorize]
[Produces("application/json")]
public class ActionItemsController : ControllerBase
{
    private const string ClosedBoardMessage = "Cannot modify items on a closed retrospective.";
    private readonly IItemService _service;
    private readonly IRetroAuthorizationService _authzService;
    private readonly IRetrospectiveLiveNotifier _liveNotifier;

    public ActionItemsController(
        IItemService service,
        IRetroAuthorizationService authzService,
        IRetrospectiveLiveNotifier liveNotifier)
    {
        _service = service;
        _authzService = authzService;
        _liveNotifier = liveNotifier;
    }

    /// <summary>Creates a new action item. The user must own the retrospective or be assigned to it.</summary>
    /// <param name="request">The action item data including assignees.</param>
    /// <returns>The ID of the newly created action item.</returns>
    [HttpPost("")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateActionItem([FromBody] Dtos.CreateActionItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var allowed = await _authzService.IsAssignedToRetrospectiveByColumnAsync(userId, request.ColumnId)
            || await _authzService.IsRetrospectiveOwnerByColumnAsync(userId, request.ColumnId);

        if (!allowed) return Forbid();
        if (await _authzService.IsRetrospectiveClosedByColumnAsync(request.ColumnId))
            return Conflict(new { message = ClosedBoardMessage });

        var svcReq = request.ToServiceRequest();
        svcReq.CreatedBy = userId;
        svcReq.CreatedByNickname = User.FindFirstValue(ClaimTypes.GivenName) ?? userId;
        var item = await _service.CreateActionItemAsync(svcReq);
        await NotifyItemsChangedAsync(request.ColumnId);
        return StatusCode(StatusCodes.Status201Created, item.Id);
    }

    /// <summary>Updates an existing action item. Creators can update their own; managers can update any on a board they own or are assigned to.</summary>
    /// <param name="id">The action item's unique identifier.</param>
    /// <param name="request">The fields to update.</param>
    /// <returns>The updated action item data.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GetActionItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateActionItem(Guid id, [FromBody] Dtos.UpdateActionItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var allowed = User.HasRole(Roles.Manager)
            ? await CanManagerActOnBoardByItemAsync(userId, id)
            : await _authzService.IsItemOwnerAsync(userId, id);

        if (!allowed) return Forbid();
        if (await _authzService.IsRetrospectiveClosedByItemAsync(id))
            return Conflict(new { message = ClosedBoardMessage });

        var item = await _service.UpdateActionItemAsync(id, request.ToServiceRequest());
        if (item is null) return NotFound();

        await NotifyItemsChangedAsync(item.ColumnId);
        return Ok(item.ToDto());
    }

    /// <summary>Marks an action item as completed. Any owner or assigned participant on the board can complete it.</summary>
    /// <param name="id">The action item's unique identifier.</param>
    /// <returns>The updated action item data.</returns>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(GetActionItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CloseActionItem(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var allowed = await _authzService.IsAssignedToRetrospectiveByItemAsync(userId, id)
            || await _authzService.IsRetrospectiveOwnerByItemAsync(userId, id);

        if (!allowed) return Forbid();
        if (await _authzService.IsRetrospectiveClosedByItemAsync(id))
            return Conflict(new { message = ClosedBoardMessage });

        var closedBy = User.FindFirstValue(ClaimTypes.Email)!;
        var item = await _service.CloseActionItemAsync(id, closedBy);
        if (item is null) return NotFound();

        await NotifyItemsChangedAsync(item.ColumnId);
        return Ok(item.ToDto());
    }

    /// <summary>Deletes an action item. Creators can delete their own; managers can delete any on a board they own or are assigned to.</summary>
    /// <param name="id">The action item's unique identifier.</param>
    /// <returns>No content if deleted, or not found if the action item does not exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteActionItem(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var allowed = User.HasRole(Roles.Manager)
            ? await CanManagerActOnBoardByItemAsync(userId, id)
            : await _authzService.IsItemOwnerAsync(userId, id);

        if (!allowed) return Forbid();
        if (await _authzService.IsRetrospectiveClosedByItemAsync(id))
            return Conflict(new { message = ClosedBoardMessage });

        var retrospectiveId = await _authzService.GetRetrospectiveIdByItemAsync(id);
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();

        if (retrospectiveId is not null)
            await _liveNotifier.NotifyItemsChangedAsync(retrospectiveId.Value);
        return NoContent();
    }

    private async Task<bool> CanManagerActOnBoardByItemAsync(string userId, Guid itemId) =>
        await _authzService.IsRetrospectiveOwnerByItemAsync(userId, itemId)
        || await _authzService.IsAssignedToRetrospectiveByItemAsync(userId, itemId);

    private async Task NotifyItemsChangedAsync(Guid columnId)
    {
        var retrospectiveId = await _authzService.GetRetrospectiveIdByColumnAsync(columnId);
        if (retrospectiveId is null) return;
        await _liveNotifier.NotifyItemsChangedAsync(retrospectiveId.Value);
    }
}

