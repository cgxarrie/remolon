using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RetroBackend.Dtos;
using RetroBackend.Mappings;
using RetroBackend.Auth;
using RetroBackend.Models;
using RetroBackend.Services;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
[Produces("application/json")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _service;
    private readonly IRetroAuthorizationService _authzService;

    public ItemsController(IItemService service, IRetroAuthorizationService authzService)
    {
        _service = service;
        _authzService = authzService;
    }

    /// <summary>Creates a new item. Standard users must be assigned; Managers must own the retrospective.</summary>
    /// <param name="request">The item data.</param>
    /// <returns>The ID of the newly created item.</returns>
    [HttpPost("")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateItem([FromBody] Dtos.CreateItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!User.IsInRole(Roles.Admin))
        {
            var allowed = User.IsInRole(Roles.Manager)
                ? await _authzService.IsRetrospectiveOwnerByColumnAsync(userId, request.ColumnId)
                : await _authzService.IsAssignedToRetrospectiveByColumnAsync(userId, request.ColumnId);

            if (!allowed) return Forbid();
        }

        var svcReq = request.ToServiceRequest();
        svcReq.CreatedBy = userId;
        svcReq.CreatedByNickname = User.FindFirstValue(ClaimTypes.GivenName) ?? userId;
        var item = await _service.CreateItemAsync(svcReq);
        return StatusCode(StatusCodes.Status201Created, item.Id);
    }

    /// <summary>Updates an existing item. Standard users can only update their own; Managers can update any in their retrospectives.</summary>
    /// <param name="id">The item's unique identifier.</param>
    /// <param name="request">The fields to update.</param>
    /// <returns>The updated item data.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GetItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] Dtos.UpdateItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!User.IsInRole(Roles.Admin))
        {
            var allowed = User.IsInRole(Roles.Manager)
                ? await _authzService.IsRetrospectiveOwnerByItemAsync(userId, id)
                : await _authzService.IsItemOwnerAsync(userId, id);

            if (!allowed) return Forbid();
        }

        var item = await _service.UpdateItemAsync(id, request.ToServiceRequest());
        return item is null ? NotFound() : Ok(item.ToDto());
    }

    /// <summary>Deletes an item. Standard users can only delete their own; Managers can delete any in their retrospectives.</summary>
    /// <param name="id">The item's unique identifier.</param>
    /// <returns>No content if deleted, or not found if the item does not exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!User.IsInRole(Roles.Admin))
        {
            var allowed = User.IsInRole(Roles.Manager)
                ? await _authzService.IsRetrospectiveOwnerByItemAsync(userId, id)
                : await _authzService.IsItemOwnerAsync(userId, id);

            if (!allowed) return Forbid();
        }

        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Unlinks an item from its group. Any user assigned to the retrospective.</summary>
    /// <param name="id">The item's unique identifier.</param>
    /// <returns>The updated item with GroupId cleared.</returns>
    [HttpDelete("{id:guid}/group")]
    [ProducesResponseType(typeof(GetItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkFromGroup(Guid id)
    {
        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var allowed = User.IsInRole(Roles.Manager)
                ? await _authzService.IsRetrospectiveOwnerByItemAsync(userId, id)
                : await _authzService.IsAssignedToRetrospectiveByItemAsync(userId, id);
            if (!allowed) return Forbid();
        }

        var item = await _service.UnlinkFromGroupAsync(id);
        return item is null ? NotFound() : Ok(item.ToDto());
    }

    /// <summary>Merges two items. Any user assigned to the retrospective.</summary>
    /// <param name="id">The source item's unique identifier.</param>
    /// <param name="request">The target item to merge with.</param>
    /// <returns>The merged group of items.</returns>
    [HttpPost("{id:guid}/merge")]
    [ProducesResponseType(typeof(IEnumerable<GetItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MergeItems(Guid id, [FromBody] Dtos.MergeItemRequest request)
    {
        if (id == request.TargetItemId)
            return BadRequest(new { message = "An item cannot be merged with itself." });

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var allowed = User.IsInRole(Roles.Manager)
                ? await _authzService.IsRetrospectiveOwnerByItemAsync(userId, id)
                : await _authzService.IsAssignedToRetrospectiveByItemAsync(userId, id);
            if (!allowed) return Forbid();
        }

        var group = await _service.MergeItemsAsync(id, request.TargetItemId);
        return group is null ? NotFound() : Ok(group.Select(i => i.ToDto()));
    }
}

