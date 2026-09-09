using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RetroBackend.Dtos;
using RetroBackend.Auth;
using RetroBackend.Config;
using RetroBackend.Models;
using RetroBackend.Data;
using RetroBackend.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private const string CouldNotCreateAccount = "Could not create account.";
    private const string ForgotPasswordMessage =
        "If an account exists for that email, a password reset link has been sent. The link expires in 30 minutes.";

    private readonly UserManager<AppUser> _userManager;
    private readonly RetroDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<AuthController> _logger;
    private readonly IAuthTokenService _authTokenService;

    public AuthController(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<AuthController> logger,
        IAuthTokenService authTokenService,
        IHostEnvironment environment)
    {
        _userManager = userManager;
        _context = context;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
        _authTokenService = authTokenService;
        _ = environment;
    }

    /// <summary>Registers a new manager and creates their organization.</summary>
    /// <param name="request">Email, password, and unique organization name for the new account.</param>
    /// <returns>A JWT token for the newly created manager.</returns>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] PublicRegisterRequest request)
    {
        var organizationName = request.OrganizationName.Trim();
        if (string.IsNullOrWhiteSpace(organizationName))
            return BadRequest(new { message = "Organization name is required." });

        var nickname = !string.IsNullOrWhiteSpace(request.Nickname)
            ? request.Nickname.Trim()
            : request.Email.Split('@')[0];
        if (await _userManager.Users.AnyAsync(u => u.Nickname.ToLower() == nickname.ToLower()))
            return BadRequest(new { message = CouldNotCreateAccount });
        if (await _context.Organizations.AnyAsync(o => o.Name.ToLower() == organizationName.ToLower()))
            return BadRequest(new { message = CouldNotCreateAccount });

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync()
            : null;
        try
        {
            var organization = new Organization { Name = organizationName };
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var user = new AppUser(request.Email, nickname) { OrganizationId = organization.Id };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();
                _logger.LogInformation(
                    "Public registration failed for {Email}: {Errors}",
                    request.Email,
                    string.Join(", ", result.Errors.Select(e => e.Code)));
                return BadRequest(new { message = CouldNotCreateAccount });
            }

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Manager);
            if (!roleResult.Succeeded)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();
                _logger.LogInformation(
                    "Public registration role assignment failed for {Email}: {Errors}",
                    request.Email,
                    string.Join(", ", roleResult.Errors.Select(e => e.Code)));
                return BadRequest(new { message = CouldNotCreateAccount });
            }

            if (transaction is not null)
                await transaction.CommitAsync();
            return TokenOk(await _authTokenService.BuildAuthResponseAsync(user, Roles.Manager));
        }
        catch (DbUpdateException ex) when (IsOrganizationNameUniqueViolation(ex))
        {
            if (transaction is not null)
                await transaction.RollbackAsync();
            return BadRequest(new { message = CouldNotCreateAccount });
        }
    }

    /// <summary>Authenticates a user and returns a JWT token.</summary>
    /// <param name="request">Email and password credentials.</param>
    /// <returns>A JWT token with the user's role.</returns>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized(new { message = "Invalid credentials." });

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var claims = await _userManager.GetClaimsAsync(user);
        var mustChangePassword = claims.Any(c =>
            c.Type == AuthClaims.MustChangePassword &&
            c.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

        if (mustChangePassword)
            return StatusCode(StatusCodes.Status403Forbidden,
                new PasswordChangeRequiredResponse("Password change required before first login.", true));

        return TokenOk(await BuildSessionAsync(user));
    }

    /// <summary>Starts forgot password flow by emailing a reset link valid for 30 minutes.</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null)
            await TrySendPasswordResetEmailAsync(user);
        else
            await PadForgotPasswordTimingAsync();

        return Ok(new ForgotPasswordResponse(ForgotPasswordMessage));
    }

    /// <summary>Completes forgot password flow by setting a new password using a reset token and signs in the user.</summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return BadRequest(new { message = "Invalid or expired reset link." });

        var tokenInput = request.Token?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tokenInput))
            return BadRequest(new { message = "Invalid or expired reset link." });

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(tokenInput));
        }
        catch
        {
            return BadRequest(new { message = "Invalid or expired reset link." });
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded && result.Errors.Any(e => e.Code == "InvalidToken"))
            result = await ResetPasswordWithInvitationTokenAsync(user, decodedToken, request.NewPassword);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "InvalidToken"))
                return BadRequest(new { message = "Invalid or expired reset link." });
            return BadRequest(result.Errors);
        }

        var mustChangePasswordClaims = (await _userManager.GetClaimsAsync(user))
            .Where(c => c.Type == AuthClaims.MustChangePassword)
            .ToList();
        if (mustChangePasswordClaims.Count > 0)
            await _userManager.RemoveClaimsAsync(user, mustChangePasswordClaims);

        await _authTokenService.RevokeAllForUserAsync(user.Id);
        return TokenOk(await BuildSessionAsync(user));
    }

    private async Task<IdentityResult> ResetPasswordWithInvitationTokenAsync(
        AppUser user,
        string token,
        string newPassword)
    {
        var valid = await _userManager.VerifyUserTokenAsync(
            user,
            InvitationTokenProviderOptions.ProviderName,
            UserManager<AppUser>.ResetPasswordTokenPurpose,
            token);
        if (!valid)
            return IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token." });

        if (await _userManager.HasPasswordAsync(user))
        {
            var removed = await _userManager.RemovePasswordAsync(user);
            if (!removed.Succeeded)
                return removed;
        }

        return await _userManager.AddPasswordAsync(user, newPassword);
    }

    /// <summary>Changes an initial temporary password and signs in the user.</summary>
    [HttpPost("change-initial-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeInitialPassword([FromBody] InitialPasswordChangeRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        if (await _userManager.IsLockedOutAsync(user))
            return Unauthorized(new { message = "Invalid credentials." });

        var claims = await _userManager.GetClaimsAsync(user);
        var mustChangePasswordClaims = claims
            .Where(c => c.Type == AuthClaims.MustChangePassword)
            .ToList();

        var mustChangePassword = mustChangePasswordClaims.Any(c =>
            c.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

        if (!mustChangePassword)
            return BadRequest(new { message = "Initial password change is not required for this user." });

        if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changeResult.Succeeded)
            return BadRequest(changeResult.Errors);

        if (mustChangePasswordClaims.Count > 0)
            await _userManager.RemoveClaimsAsync(user, mustChangePasswordClaims);

        return TokenOk(await BuildSessionAsync(user));
    }

    /// <summary>Exchanges a refresh token for a new access token.</summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest? request)
    {
        var presented = request?.RefreshToken;
        if (string.IsNullOrWhiteSpace(presented))
            Request.Cookies.TryGetValue(AuthCookies.Refresh, out presented);
        if (string.IsNullOrWhiteSpace(presented))
            return Unauthorized(new { message = "Invalid credentials." });

        var response = await _authTokenService.RefreshAsync(presented);
        if (response is null)
            return Unauthorized(new { message = "Invalid credentials." });
        return TokenOk(response);
    }

    /// <summary>Revokes the supplied refresh token.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
    {
        var presented = request?.RefreshToken;
        if (string.IsNullOrWhiteSpace(presented))
            Request.Cookies.TryGetValue(AuthCookies.Refresh, out presented);
        if (!string.IsNullOrWhiteSpace(presented))
            await _authTokenService.RevokeAsync(presented);
        AuthCookies.Clear(Response, Request.IsHttps);
        return NoContent();
    }

    /// <summary>Changes the authenticated user's password and issues a fresh session.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
            return BadRequest(new { message = "Could not change password." });

        var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changeResult.Succeeded)
            return BadRequest(changeResult.Errors);

        await _authTokenService.RevokeAllForUserAsync(user.Id);
        return TokenOk(await BuildSessionAsync(user));
    }

    private async Task<AuthTokenResponse> BuildSessionAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.StandardUser;
        return await _authTokenService.BuildAuthResponseAsync(user, role);
    }

    private IActionResult TokenOk(AuthTokenResponse response)
    {
        AuthCookies.Append(Response, Request.IsHttps, response);
        return Ok(response);
    }

    private async Task PadForgotPasswordTimingAsync()
    {
        var pad = new AppUser($"pad-{Guid.NewGuid():N}@invalid", "pad");
        try
        {
            await _userManager.GeneratePasswordResetTokenAsync(pad);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Forgot-password timing pad failed.");
        }
    }

    private async Task TrySendPasswordResetEmailAsync(AppUser user)
    {
        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetUrl = PasswordResetEmail.BuildResetUrl(_emailOptions.FrontendBaseUrl, user.Email!, encodedToken);
            await _emailSender.SendAsync(
                user.Email!,
                PasswordResetEmail.Subject,
                PasswordResetEmail.HtmlBody(resetUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email");
        }
    }

    private static bool IsOrganizationNameUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
