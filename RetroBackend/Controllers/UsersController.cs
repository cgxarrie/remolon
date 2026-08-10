using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using RetroBackend.Dtos;
using RetroBackend.Auth;
using RetroBackend.Models;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public UsersController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Creates a user with a temporary password. Admin and Manager.</summary>
    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var isManager = User.HasRole(Roles.Manager) && !User.HasRole(Roles.Admin);
        var role = string.IsNullOrWhiteSpace(request.Role) ? Roles.StandardUser : request.Role.Trim();

        if (isManager && role != Roles.StandardUser)
            return Forbid();

        if (role != Roles.Admin && role != Roles.Manager && role != Roles.StandardUser)
            return BadRequest(new { message = "Role must be Admin, Manager, or StandardUser." });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return BadRequest(new { message = "A user with this email already exists." });

        var atIndex = request.Email.IndexOf('@');
        var fallbackNickname = atIndex > 0 ? request.Email[..atIndex] : request.Email;
        var nickname = string.IsNullOrWhiteSpace(request.Nickname)
            ? fallbackNickname
            : request.Nickname.Trim();

        var temporaryPassword = GenerateTemporaryPassword();

        var user = new AppUser(request.Email, nickname);
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
    [ProducesResponseType(typeof(IEnumerable<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        var result = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserSummaryDto(user.Id, user.Email!, user.Nickname, roles.FirstOrDefault() ?? Roles.StandardUser));
        }

        return Ok(result);
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
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (id == currentUserId)
            return Forbid();

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var addResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
            return BadRequest(addResult.Errors);

        return Ok(new UserSummaryDto(user.Id, user.Email!, user.Nickname, request.Role));
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
