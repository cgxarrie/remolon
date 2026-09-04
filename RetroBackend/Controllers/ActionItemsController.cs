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
    /// <param name="request">The action item data including assignee.</param>
    /// <returns>The ID of the newly created action item.</returns>
    [HttpPost("")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateActionItem([FromBody] Dtos.CreateActionItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!User.HasRole(Roles.Admin))
        {
            var allowed = await _authzService.IsAssignedToRetrospectiveByColumnAsync(userId, request.ColumnId)
                || await _authzService.IsRetrospectiveOwnerByColumnAsync(userId, request.ColumnId);

            if (!allowed) return Forbid();
        }

        var svcReq = request.ToServiceRequest();
        svcReq.CreatedBy = userId;
        svcReq.CreatedByNickname = User.FindFirstValue(ClaimTypes.GivenName) ?? userId;
        var item = await _service.CreateActionItemAsync(svcReq);
        await NotifyItemsChangedAsync(request.ColumnId);
        return StatusCode(StatusCodes.Status201Created, item.Id);
    }

    /// <summary>Updates an existing action item. Standard users can only update their own; Managers can update any in their retrospectives.</summary>
    /// <param name="id">The action item's unique identifier.</param>
    /// <param name="request">The fields to update.</param>
    /// <returns>The updated action item data.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GetActionItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateActionItem(Guid id, [FromBody] Dtos.UpdateActionItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!User.HasRole(Roles.Admin))
        {
            var allowed = User.HasRole(Roles.Manager)
                ? await _authzService.IsRetrospectiveOwnerByItemAsync(userId, id)
                : await _authzService.IsItemOwnerAsync(userId, id);

            if (!allowed) return Forbid();
        }

        var item = await _service.UpdateActionItemAsync(id, request.ToServiceRequest());
        if (item is null) return NotFound();

        await NotifyItemsChangedAsync(item.ColumnId);
        return Ok(item.ToDto());
    }

    /// <summary>Marks an action item as completed.</summary>
    /// <param name="id">The action item's unique identifier.</param>
    /// <returns>The updated action item data.</returns>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(GetActionItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseActionItem(Guid id)
    {
        var closedBy = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email)!;
        var item = await _service.CloseActionItemAsync(id, closedBy);
        if (item is null) return NotFound();

        await NotifyItemsChangedAsync(item.ColumnId);
        return Ok(item.ToDto());
    }

    /// <summary>Deletes an action item. Standard users can only delete their own; Managers can delete any in their retrospectives.</summary>
    /// <param name="id">The action item's unique identifier.</param>
    /// <returns>No content if deleted, or not found if the action item does not exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteActionItem(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!User.HasRole(Roles.Admin))
        {
            var allowed = User.HasRole(Roles.Manager)
                ? await _authzService.IsRetrospectiveOwnerByItemAsync(userId, id)
                : await _authzService.IsItemOwnerAsync(userId, id);

            if (!allowed) return Forbid();
        }

        var retrospectiveId = await _authzService.GetRetrospectiveIdByItemAsync(id);
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();

        if (retrospectiveId is not null)
            await _liveNotifier.NotifyItemsChangedAsync(retrospectiveId.Value);
        return NoContent();
    }

    private async Task NotifyItemsChangedAsync(Guid columnId)
    {
        var retrospectiveId = await _authzService.GetRetrospectiveIdByColumnAsync(columnId);
        if (retrospectiveId is null) return;
        await _liveNotifier.NotifyItemsChangedAsync(retrospectiveId.Value);
    }
}

