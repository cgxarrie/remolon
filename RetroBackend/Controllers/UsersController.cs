using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RetroBackend.Dtos;
using RetroBackend.Auth;
using RetroBackend.Config;
using RetroBackend.Models;
using RetroBackend.Data;
using RetroBackend.Services;
using System.Security.Claims;
using System.Text;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RetroDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<UsersController> _logger;
    private readonly IAuthTokenService _authTokenService;

    public UsersController(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<UsersController> logger,
        IAuthTokenService authTokenService)
    {
        _userManager = userManager;
        _context = context;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
        _authTokenService = authTokenService;
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await FindCurrentUserAsync();
        if (user is null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.StandardUser;
        return Ok(new CurrentUserDto(user.Email!, user.Nickname, role, AvatarImage.UrlFor(user)));
    }

    /// <summary>Updates the authenticated user's nickname and returns a new JWT.</summary>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request)
    {
        var user = await FindCurrentUserAsync();
        if (user is null) return Unauthorized();

        var nickname = request.Nickname.Trim();
        if (string.IsNullOrWhiteSpace(nickname))
            return BadRequest(new { message = "Nickname is required." });

        if (!string.Equals(user.Nickname, nickname, StringComparison.OrdinalIgnoreCase)
            && await NicknameTakenAsync(nickname))
            return BadRequest(new { message = "A user with this nickname already exists." });

        user.Nickname = nickname;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.StandardUser;
        return Ok(await _authTokenService.BuildAuthResponseAsync(user, role));
    }

    /// <summary>Creates a user and emails a one-time set-password link. Manager.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var role = string.IsNullOrWhiteSpace(request.Role) ? Roles.StandardUser : request.Role.Trim();
        if (role != Roles.StandardUser)
            return Forbid();

        if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var organizationId))
            return Forbid();
        if (!await _context.Organizations.AnyAsync(o => o.Id == organizationId))
            return BadRequest(new { message = "A valid organizationId is required." });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return BadRequest(new { message = "A user with this email already exists." });

        var atIndex = request.Email.IndexOf('@');
        var fallbackNickname = atIndex > 0 ? request.Email[..atIndex] : request.Email;
        string nickname;
        if (!string.IsNullOrWhiteSpace(request.Nickname))
        {
            nickname = request.Nickname.Trim();
            if (await NicknameTakenAsync(nickname))
                return BadRequest(new { message = "A user with this nickname already exists." });
        }
        else
        {
            nickname = await UniqueNicknameAsync(fallbackNickname);
        }

        var user = new AppUser(request.Email, nickname) { OrganizationId = organizationId };
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors);

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            return BadRequest(roleResult.Errors);

        await _userManager.AddClaimAsync(user, new Claim(AuthClaims.MustChangePassword, "true"));

        var invitationEmailSent = await TrySendInvitationAsync(user);

        return StatusCode(StatusCodes.Status201Created,
            new CreateUserResponse(
                user.Id,
                user.Email!,
                user.Nickname,
                role,
                invitationEmailSent));
    }

    /// <summary>Returns all users with their assigned role. Manager.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(typeof(PagedResponse<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid? organizationId, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var ownOrganizationId))
            return Forbid();
        if (organizationId.HasValue && organizationId != ownOrganizationId) return Forbid();
        organizationId = ownOrganizationId;

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
                roles.FirstOrDefault() ?? Roles.StandardUser, user.OrganizationId, user.Organization?.Name,
                AvatarImage.UrlFor(user)));
        }

        return Ok(new PagedResponse<UserSummaryDto>(result, page, pageSize, totalCount));
    }

    /// <summary>Changes the role of a user in the manager's organization. Cannot change own role.</summary>
    [HttpPatch("{id}/role")]
    [Authorize(Roles = Roles.Manager)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleRequest request)
    {
        if (request.Role != Roles.Manager && request.Role != Roles.StandardUser)
            return BadRequest(new { message = "Role must be Manager or StandardUser." });

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (id == currentUserId)
            return Forbid();

        if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var organizationId))
            return Forbid();

        var user = await _userManager.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (user.OrganizationId != organizationId)
            return Forbid();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        var addResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
            return BadRequest(addResult.Errors);

        return Ok(new UserSummaryDto(user.Id, user.Email!, user.Nickname, request.Role,
            user.OrganizationId, user.Organization?.Name, AvatarImage.UrlFor(user)));
    }

    /// <summary>Deletes a user. Manager. Cannot delete self.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Manager)]
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
        if (!Guid.TryParse(User.FindFirstValue(AuthClaims.OrganizationId), out var organizationId)
            || user.OrganizationId != organizationId)
            return Forbid();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    private async Task<AppUser?> FindCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        return await _userManager.FindByIdAsync(userId);
    }

    private async Task<bool> NicknameTakenAsync(string nickname) =>
        await _userManager.Users.AnyAsync(u => u.Nickname.ToLower() == nickname.ToLower());

    private async Task<string> UniqueNicknameAsync(string preferred)
    {
        var candidate = preferred;
        var suffix = 2;
        while (await NicknameTakenAsync(candidate))
        {
            candidate = $"{preferred}{suffix}";
            suffix += 1;
        }
        return candidate;
    }

    private async Task<bool> TrySendInvitationAsync(AppUser user)
    {
        try
        {
            var token = await _userManager.GenerateUserTokenAsync(
                user,
                InvitationTokenProviderOptions.ProviderName,
                UserManager<AppUser>.ResetPasswordTokenPurpose);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var setPasswordUrl = PasswordResetEmail.BuildResetUrl(
                _emailOptions.FrontendBaseUrl,
                user.Email!,
                encodedToken,
                invite: true);
            await _emailSender.SendAsync(
                user.Email!,
                UserInvitationEmail.Subject,
                UserInvitationEmail.HtmlBody(user.Email!, setPasswordUrl));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {Email}", user.Email);
            return false;
        }
    }
}
