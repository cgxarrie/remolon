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

    /// <summary>Assigns a user to a retrospective. Admin and Manager.</summary>
    /// <param name="request">The user email and retrospective ID.</param>
    /// <returns>Confirmation message.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignUser([FromBody] AssignUserRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.UserEmail);
        if (user is null) return NotFound(new { message = "User not found." });

        var retroExists = await _context.Retrospectives.AnyAsync(r => r.Id == request.RetrospectiveId);
        if (!retroExists) return NotFound(new { message = "Retrospective not found." });

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

    /// <summary>Removes a user's assignment from a retrospective. Admin and Manager.</summary>
    /// <param name="request">The user email and retrospective ID.</param>
    /// <returns>No content if removed.</returns>
    [HttpDelete]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
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

        _context.UserRetrospectives.Remove(assignment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Lists all retrospectives a user is assigned to. Admin only.</summary>
    /// <param name="userEmail">The user's email address.</param>
    /// <returns>List of retrospective IDs.</returns>
    [HttpGet("{userEmail}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserAssignments(string userEmail)
    {
        var user = await _userManager.FindByEmailAsync(userEmail);
        if (user is null) return NotFound(new { message = "User not found." });

        var assignments = await _context.UserRetrospectives
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RetrospectiveId)
            .ToListAsync();

        return Ok(assignments);
    }

    /// <summary>Lists users assigned to a specific retrospective.</summary>
    /// <param name="retrospectiveId">The retrospective's unique identifier.</param>
    /// <returns>List of assigned participants.</returns>
    [HttpGet("retrospective/{retrospectiveId:guid}/participants")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Manager + "," + Roles.StandardUser)]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRetrospectiveParticipants(Guid retrospectiveId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.HasRole(Roles.Admin);
        var isManager = User.HasRole(Roles.Manager);

        if (!isAdmin && !isManager)
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
                role
            ));
        }

        return Ok(result.OrderBy(u => u.Nickname));
    }
}
