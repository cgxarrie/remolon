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
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class RetrospectivesController : ControllerBase
{
    private readonly IRetrospectiveService _service;
    private readonly IRetroAuthorizationService _authzService;

    public RetrospectivesController(IRetrospectiveService service, IRetroAuthorizationService authzService)
    {
        _service = service;
        _authzService = authzService;
    }

    /// <summary>Retrieves all retrospectives.</summary>
    /// <returns>A list of retrospectives with their basic information.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GetRetrospectiveSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        IEnumerable<Retrospective> items;
        if (User.HasRole(Roles.Admin))
        {
            items = await _service.GetAllAsync();
        }
        else
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            items = await _service.GetAllForUserAsync(userId);
        }
        return Ok(items.Select(r => r.ToSummaryDto()));
    }

    /// <summary>Retrieves a single retrospective by its ID.</summary>
    /// <param name="id">The retrospective's unique identifier.</param>
    /// <returns>The retrospective data including its columns and items.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetRetrospectiveDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!User.HasRole(Roles.Admin))
        {
            var isOwner = await _authzService.IsRetrospectiveOwnerAsync(userId, id);
            var isAssigned = await _authzService.IsAssignedToRetrospectiveAsync(userId, id);
            if (!isOwner && !isAssigned) return Forbid();
        }

        var retro = await _service.GetByIdAsync(id);
        return retro is null ? NotFound() : Ok(retro.ToGetDto(userId));
    }

    /// <summary>Creates a new retrospective. Admin or Manager.</summary>
    /// <param name="request">The retrospective data.</param>
    /// <returns>The ID of the newly created retrospective.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] Dtos.CreateRetrospectiveRequest request)
    {
        var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var svcReq = request.ToServiceRequest();
        svcReq.CurrentUser = createdBy;
        var retro = await _service.CreateAsync(svcReq);
        return CreatedAtAction(nameof(GetById), new { id = retro.Id }, retro.Id);
    }

    /// <summary>Partially updates an existing retrospective. Admin always; Manager for own or assigned retrospectives.</summary>
    /// <param name="id">The retrospective's unique identifier.</param>
    /// <param name="request">Fields to update.</param>
    /// <returns>The ID of the updated retrospective.</returns>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] Dtos.UpdateRetrospectiveRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (User.HasRole(Roles.Manager) && !User.HasRole(Roles.Admin))
        {
            var isOwner = await _authzService.IsRetrospectiveOwnerAsync(userId, id);
            var isAssigned = await _authzService.IsAssignedToRetrospectiveAsync(userId, id);
            if (!isOwner && !isAssigned) return Forbid();
        }

        var svcReq = request.ToServiceRequest();
        svcReq.CurrentUser = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var retro = await _service.UpdateAsync(id, svcReq);
        return retro is null ? NotFound() : Ok(retro.Id);
    }

    /// <summary>Deletes a retrospective. Admin always; Manager for own or assigned retrospectives.</summary>
    /// <param name="id">The retrospective's unique identifier.</param>
    /// <returns>No content if deleted, or not found if the retrospective does not exist.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (User.HasRole(Roles.Manager) && !User.HasRole(Roles.Admin))
        {
            var isOwner = await _authzService.IsRetrospectiveOwnerAsync(userId, id);
            var isAssigned = await _authzService.IsAssignedToRetrospectiveAsync(userId, id);
            if (!isOwner && !isAssigned) return Forbid();
        }

        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Closes a retrospective and creates the next one. Admin always; Manager for own or assigned retrospectives.</summary>
    /// <param name="id">The retrospective's unique identifier.</param>
    /// <param name="request">Close request data.</param>
    /// <returns>The ID of the newly created retrospective.</returns>
    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(Guid id, [FromBody] Dtos.CloseRetrospectiveRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (User.HasRole(Roles.Manager) && !User.HasRole(Roles.Admin))
        {
            var isOwner = await _authzService.IsRetrospectiveOwnerAsync(userId, id);
            var isAssigned = await _authzService.IsAssignedToRetrospectiveAsync(userId, id);
            if (!isOwner && !isAssigned) return Forbid();
        }

        var svcReq = request.ToServiceRequest();
        svcReq.CurrentUser = userId;

        var newRetro = await _service.CloseAsync(id, svcReq);
        if (newRetro is null)
        {
            var existing = await _service.GetByIdAsync(id);
            return existing is null ? NotFound() : Conflict(new { message = "Retrospective is already closed." });
        }
        return CreatedAtAction(nameof(GetById), new { id = newRetro.Id }, newRetro.Id);
    }

    /// <summary>Reveals a retrospective so all participants can see all tickets.</summary>
    /// <param name="id">The retrospective's unique identifier.</param>
    /// <returns>The ID of the revealed retrospective.</returns>
    [HttpPost("{id:guid}/reveal")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reveal(Guid id)
    {
        if (User.HasRole(Roles.Manager) && !User.HasRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isOwner = await _authzService.IsRetrospectiveOwnerAsync(userId, id);
            var isAssigned = await _authzService.IsAssignedToRetrospectiveAsync(userId, id);
            if (!isOwner && !isAssigned) return Forbid();
        }

        var revealed = await _service.RevealAsync(id);
        return revealed is null ? NotFound() : Ok(revealed.Id);
    }
}

