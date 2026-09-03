using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RetroBackend.Dtos;
using RetroBackend.Mappings;
using RetroBackend.Auth;
using RetroBackend.Models;
using RetroBackend.Services;
using RetroBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class RetrospectivesController : ControllerBase
{
    private readonly IRetrospectiveService _service;
    private readonly IRetroAuthorizationService _authzService;
    private readonly RetroDbContext _context;

    public RetrospectivesController(IRetrospectiveService service, IRetroAuthorizationService authzService, RetroDbContext context)
    {
        _service = service;
        _authzService = authzService;
        _context = context;
    }

    /// <summary>Retrieves all retrospectives.</summary>
    /// <returns>A list of retrospectives with their basic information.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<GetRetrospectiveSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid? organizationId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var isAdmin = User.HasRole(Roles.Admin);
        if (isAdmin)
        {
            if (!organizationId.HasValue)
                return BadRequest(new { message = "organizationId is required." });
        }
        else
        {
            if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var ownOrganizationId))
                return Forbid();
            if (organizationId.HasValue && organizationId != ownOrganizationId) return Forbid();
            organizationId = ownOrganizationId;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = _context.Retrospectives.Include(r => r.Organization)
            .Where(r => r.OrganizationId == organizationId);
        if (!isAdmin)
            query = query.Where(r => r.CreatedBy == userId
                || _context.UserRetrospectives.Any(ur => ur.RetrospectiveId == r.Id && ur.UserId == userId));

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(r => r.Title.ToLower()).ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResponse<GetRetrospectiveSummaryDto>(
            items.Select(r => r.ToSummaryDto()).ToList(), page, pageSize, totalCount));
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
        if (retro is not null && !User.HasRole(Roles.Admin) && !IsSameOrganization(retro.OrganizationId))
            return Forbid();
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
        if (User.HasRole(Roles.Admin))
        {
            if (!request.OrganizationId.HasValue
                || !await _context.Organizations.AnyAsync(o => o.Id == request.OrganizationId))
                return BadRequest(new { message = "A valid organizationId is required." });
            svcReq.OrganizationId = request.OrganizationId.Value;
        }
        else
        {
            if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var organizationId))
                return Forbid();
            svcReq.OrganizationId = organizationId;
        }

        var managerUserIds = request.ManagerUserIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        if (managerUserIds.Count == 0)
            return BadRequest(new { message = "At least one Manager must be assigned." });

        var managers = await _context.Users
            .Where(user => managerUserIds.Contains(user.Id) && user.OrganizationId == svcReq.OrganizationId)
            .ToListAsync();
        if (managers.Count != managerUserIds.Count)
            return BadRequest(new { message = "All assigned managers must belong to the retrospective's organization." });

        foreach (var manager in managers)
        {
            if (!await _context.UserRoles
                .Where(userRole => userRole.UserId == manager.Id)
                .Join(
                    _context.Roles.Where(role => role.NormalizedName == Roles.Manager.ToUpper()),
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (_, _) => true)
                .AnyAsync())
                return BadRequest(new { message = "Every assigned manager must have the Manager role." });
        }

        var retro = await _service.CreateAsync(svcReq);
        _context.UserRetrospectives.AddRange(managerUserIds.Select(managerUserId => new UserRetrospective
        {
            UserId = managerUserId,
            RetrospectiveId = retro.Id,
        }));
        await _context.SaveChangesAsync();
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
            var existing = await _service.GetByIdAsync(id);
            if (existing is null) return NotFound();
            if (!IsSameOrganization(existing.OrganizationId)) return Forbid();
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
            var existing = await _service.GetByIdAsync(id);
            if (existing is null) return NotFound();
            if (!IsSameOrganization(existing.OrganizationId)) return Forbid();
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
            var existingRetro = await _service.GetByIdAsync(id);
            if (existingRetro is null) return NotFound();
            if (!IsSameOrganization(existingRetro.OrganizationId)) return Forbid();
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
            var existing = await _service.GetByIdAsync(id);
            if (existing is null) return NotFound();
            if (!IsSameOrganization(existing.OrganizationId)) return Forbid();
            var isOwner = await _authzService.IsRetrospectiveOwnerAsync(userId, id);
            var isAssigned = await _authzService.IsAssignedToRetrospectiveAsync(userId, id);
            if (!isOwner && !isAssigned) return Forbid();
        }

        var revealed = await _service.RevealAsync(id);
        return revealed is null ? NotFound() : Ok(revealed.Id);
    }

    private bool IsSameOrganization(Guid organizationId) =>
        Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var currentOrganizationId)
        && currentOrganizationId == organizationId;
}

