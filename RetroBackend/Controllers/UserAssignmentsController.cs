using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Auth;
using RetroBackend.Models;
using RetroBackend.Services;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/user-assignments")]
[Authorize]
[Produces("application/json")]
public class UserAssignmentsController : ControllerBase
{
    private readonly RetroDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IRetroAuthorizationService _authzService;

    public UserAssignmentsController(RetroDbContext context, UserManager<AppUser> userManager, IRetroAuthorizationService authzService)
    {
        _context = context;
        _userManager = userManager;
        _authzService = authzService;
    }

    /// <summary>Assigns a user to a retrospective. Manager.</summary>
    /// <param name="request">The user email and retrospective ID.</param>
    /// <returns>Confirmation message.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignUser([FromBody] AssignUserRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.UserEmail);
        if (user is null) return NotFound(new { message = "User not found." });

        var retro = await _context.Retrospectives.FindAsync(request.RetrospectiveId);
        if (retro is null) return NotFound(new { message = "Retrospective not found." });
        if (user.OrganizationId is null || user.OrganizationId != retro.OrganizationId)
            return BadRequest(new { message = "User and retrospective must belong to the same organization." });
        if (User.HasRole(Roles.Manager)
            && (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var organizationId)
                || organizationId != retro.OrganizationId))
            return Forbid();

        var alreadyAssigned = await _context.UserRetrospectives
            .AnyAsync(ur => ur.UserId == user.Id && ur.RetrospectiveId == request.RetrospectiveId);
        if (alreadyAssigned)
            return Conflict(new { message = "User is already assigned to this retrospective." });

        _context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = user.Id,
            RetrospectiveId = request.RetrospectiveId
        });
        await _context.SaveChangesAsync();

        return Ok(new { message = $"User '{user.Email}' assigned to retrospective {request.RetrospectiveId}." });
    }

    /// <summary>Replaces a retrospective's participant assignments.</summary>
    /// <param name="request">The complete desired set of user IDs and retrospective ID.</param>
    /// <returns>The numbers of assigned and removed users.</returns>
    [HttpPost("batch")]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignUsers([FromBody] BatchAssignUsersRequest request)
    {
        var retro = await _context.Retrospectives.FindAsync(request.RetrospectiveId);
        if (retro is null) return NotFound(new { message = "Retrospective not found." });
        if (!CanManage(retro.OrganizationId)) return Forbid();

        var userIds = request.UserIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        if (userIds.Count == 0)
            return BadRequest(new { message = "At least one Manager must remain assigned to the retrospective." });

        var users = await _userManager.Users
            .Where(user => userIds.Contains(user.Id) && user.OrganizationId == retro.OrganizationId)
            .ToListAsync();
        if (users.Count != userIds.Count)
            return BadRequest(new { message = "All users must exist and belong to the retrospective's organization." });

        var selectedManagerCount = 0;
        foreach (var user in users)
        {
            if (await _userManager.IsInRoleAsync(user, Roles.Manager))
                selectedManagerCount++;
        }
        if (selectedManagerCount == 0)
            return BadRequest(new { message = "At least one Manager must remain assigned to the retrospective." });

        var currentAssignments = await _context.UserRetrospectives
            .Where(assignment => assignment.RetrospectiveId == request.RetrospectiveId)
            .ToListAsync();
        var assignedUserIdSet = currentAssignments.Select(assignment => assignment.UserId).ToHashSet();
        var desiredUserIdSet = userIds.ToHashSet();
        var assignments = users
            .Where(user => !assignedUserIdSet.Contains(user.Id))
            .Select(user => new UserRetrospective
            {
                UserId = user.Id,
                RetrospectiveId = request.RetrospectiveId
            })
            .ToList();
        var removals = currentAssignments
            .Where(assignment => !desiredUserIdSet.Contains(assignment.UserId))
            .ToList();

        _context.UserRetrospectives.AddRange(assignments);
        _context.UserRetrospectives.RemoveRange(removals);
        await _context.SaveChangesAsync();

        return Ok(new { assignedCount = assignments.Count, removedCount = removals.Count });
    }

    /// <summary>Removes a user's assignment from a retrospective. Manager.</summary>
    /// <param name="request">The user email and retrospective ID.</param>
    /// <returns>No content if removed.</returns>
    [HttpDelete]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnassignUser([FromBody] AssignUserRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.UserEmail);
        if (user is null) return NotFound(new { message = "User not found." });

        var assignment = await _context.UserRetrospectives
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RetrospectiveId == request.RetrospectiveId);
        if (assignment is null) return NotFound(new { message = "Assignment not found." });
        var retro = await _context.Retrospectives.FindAsync(request.RetrospectiveId);
        if (retro is null || user.OrganizationId != retro.OrganizationId)
            return BadRequest(new { message = "User and retrospective must belong to the same organization." });
        if (User.HasRole(Roles.Manager)
            && (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var organizationId)
                || organizationId != retro.OrganizationId))
            return Forbid();

        if (await _userManager.IsInRoleAsync(user, Roles.Manager))
        {
            var otherAssignedUsers = await _context.UserRetrospectives
                .Where(ur => ur.RetrospectiveId == request.RetrospectiveId && ur.UserId != user.Id)
                .Select(ur => ur.User)
                .ToListAsync();
            var hasAnotherManager = false;
            foreach (var otherUser in otherAssignedUsers)
            {
                if (await _userManager.IsInRoleAsync(otherUser, Roles.Manager))
                {
                    hasAnotherManager = true;
                    break;
                }
            }
            if (!hasAnotherManager)
                return BadRequest(new { message = "The last assigned Manager cannot be removed." });
        }

        _context.UserRetrospectives.Remove(assignment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Lists users assigned to a specific retrospective.</summary>
    /// <param name="retrospectiveId">The retrospective's unique identifier.</param>
    /// <returns>List of assigned participants.</returns>
    [HttpGet("retrospective/{retrospectiveId:guid}/participants")]
    [Authorize(Roles = Roles.Manager + "," + Roles.StandardUser)]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRetrospectiveParticipants(Guid retrospectiveId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isManager = User.HasRole(Roles.Manager);

        if (!isManager)
        {
            var isOwner = await _authzService.IsRetrospectiveOwnerAsync(currentUserId, retrospectiveId);
            var isAssigned = await _context.UserRetrospectives
                .AnyAsync(ur => ur.UserId == currentUserId && ur.RetrospectiveId == retrospectiveId);

            if (!isOwner && !isAssigned)
                return Forbid();
        }

        var users = await _context.UserRetrospectives
            .Where(ur => ur.RetrospectiveId == retrospectiveId)
            .Select(ur => ur.User)
            .Distinct()
            .ToListAsync();

        var result = new List<UserSummaryDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? Roles.StandardUser;
            result.Add(new UserSummaryDto(
                user.Id,
                user.Email ?? string.Empty,
                user.Nickname,
                role,
                user.OrganizationId,
                user.Organization?.Name,
                AvatarImage.UrlFor(user)
            ));
        }

        return Ok(result.OrderBy(u => u.Nickname));
    }

    /// <summary>Lists all users eligible for assignment to a retrospective.</summary>
    /// <param name="retrospectiveId">The retrospective's unique identifier.</param>
    /// <returns>All users in the retrospective's organization.</returns>
    [HttpGet("retrospective/{retrospectiveId:guid}/users")]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRetrospectiveUsers(Guid retrospectiveId)
    {
        var retro = await _context.Retrospectives.FindAsync(retrospectiveId);
        if (retro is null) return NotFound(new { message = "Retrospective not found." });
        if (!CanManage(retro.OrganizationId)) return Forbid();

        var users = await _userManager.Users
            .Include(user => user.Organization)
            .Where(user => user.OrganizationId == retro.OrganizationId)
            .OrderBy(user => user.Nickname.ToLower())
            .ThenBy(user => user.Email)
            .ToListAsync();
        var result = new List<UserSummaryDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserSummaryDto(
                user.Id,
                user.Email ?? string.Empty,
                user.Nickname,
                roles.FirstOrDefault() ?? Roles.StandardUser,
                user.OrganizationId,
                user.Organization?.Name,
                AvatarImage.UrlFor(user)
            ));
        }

        return Ok(result);
    }

    private bool CanManage(Guid organizationId) =>
        User.HasRole(Roles.Manager)
        && Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var managerOrganizationId)
        && managerOrganizationId == organizationId;
}
