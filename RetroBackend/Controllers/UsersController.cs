using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using RetroBackend.Dtos;
using RetroBackend.Auth;
using RetroBackend.Models;
using RetroBackend.Data;
using System.Security.Claims;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RetroDbContext _context;

    public UsersController(UserManager<AppUser> userManager, RetroDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    /// <summary>Creates a user with a temporary password. Admin and Manager.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var isAdmin = User.HasRole(Roles.Admin);
        var role = string.IsNullOrWhiteSpace(request.Role) ? Roles.StandardUser : request.Role.Trim();

        if (role == Roles.Admin && !isAdmin)
            return Forbid();

        if (!isAdmin && role != Roles.StandardUser)
            return Forbid();

        if (role != Roles.Admin && role != Roles.Manager && role != Roles.StandardUser)
            return BadRequest(new { message = "Role must be Admin, Manager, or StandardUser." });

        Guid? organizationId = null;
        if (role != Roles.Admin)
        {
            if (!isAdmin)
            {
                if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var managerOrganizationId))
                    return Forbid();
                organizationId = managerOrganizationId;
            }
            else
            {
                organizationId = request.OrganizationId;
            }

            if (organizationId is null || !await _context.Organizations.AnyAsync(o => o.Id == organizationId))
                return BadRequest(new { message = "A valid organizationId is required." });
        }

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return BadRequest(new { message = "A user with this email already exists." });

        var atIndex = request.Email.IndexOf('@');
        var fallbackNickname = atIndex > 0 ? request.Email[..atIndex] : request.Email;
        var nickname = string.IsNullOrWhiteSpace(request.Nickname)
            ? fallbackNickname
            : request.Nickname.Trim();
        if (await _userManager.Users.AnyAsync(u => u.Nickname.ToLower() == nickname.ToLower()))
            return BadRequest(new { message = "A user with this nickname already exists." });

        var temporaryPassword = GenerateTemporaryPassword();

        var user = new AppUser(request.Email, nickname) { OrganizationId = organizationId };
        var createResult = await _userManager.CreateAsync(user, temporaryPassword);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors);

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            return BadRequest(roleResult.Errors);

        await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim(AuthClaims.MustChangePassword, "true"));

        return StatusCode(StatusCodes.Status201Created,
            new CreateUserResponse(user.Id, user.Email!, user.Nickname, role, temporaryPassword));
    }

    /// <summary>Returns all users with their assigned role. Admin and Manager.</summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(typeof(PagedResponse<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid? organizationId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var isAdmin = User.HasRole(Roles.Admin);
        if (!isAdmin)
        {
            if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var ownOrganizationId))
                return Forbid();
            if (organizationId.HasValue && organizationId != ownOrganizationId) return Forbid();
            organizationId = ownOrganizationId;
        }
        else if (!organizationId.HasValue)
        {
            return BadRequest(new { message = "organizationId is required." });
        }

        var query = _userManager.Users.Include(u => u.Organization)
            .Where(u => u.OrganizationId == organizationId);
        var totalCount = await query.CountAsync();
        var users = await query.OrderBy(u => u.Nickname.ToLower()).ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var result = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserSummaryDto(user.Id, user.Email!, user.Nickname,
                roles.FirstOrDefault() ?? Roles.StandardUser, user.OrganizationId, user.Organization?.Name));
        }

        return Ok(new PagedResponse<UserSummaryDto>(result, page, pageSize, totalCount));
    }

    /// <summary>Changes the role of a user. Admin only. Cannot demote self.</summary>
    [HttpPatch("{id}/role")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleRequest request)
    {
        if (request.Role == Roles.Admin && !User.HasRole(Roles.Admin))
            return Forbid();

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (id == currentUserId)
            return Forbid();

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (request.Role != Roles.Admin && user.OrganizationId is null)
            return BadRequest(new { message = "A Manager or StandardUser must belong to an organization." });

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var addResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
            return BadRequest(addResult.Errors);

        if (request.Role == Roles.Admin)
        {
            user.OrganizationId = null;
            await _userManager.UpdateAsync(user);
        }
        return Ok(new UserSummaryDto(user.Id, user.Email!, user.Nickname, request.Role,
            user.OrganizationId, user.Organization?.Name));
    }

    /// <summary>Deletes a user. Admin and Manager. Cannot delete self.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (id == currentUserId)
            return Forbid();

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (User.HasRole(Roles.Manager) && !User.HasRole(Roles.Admin))
        {
            if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var organizationId)
                || user.OrganizationId != organizationId)
                return Forbid();
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    private static string GenerateTemporaryPassword(int length = 12)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string all = lowercase + uppercase + digits;

        var chars = new char[length];
        chars[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        chars[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

        for (var i = 3; i < length; i += 1)
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        for (var i = chars.Length - 1; i > 0; i -= 1)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
