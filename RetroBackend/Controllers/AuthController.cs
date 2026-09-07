using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
    private const string ForgotPasswordMessage =
        "If an account exists for that email, a password reset link has been sent. The link expires in 30 minutes.";

    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly RetroDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        RetroDbContext context,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    /// <summary>Registers a new manager and creates their organization.</summary>
    /// <param name="request">Email, password, and unique organization name for the new account.</param>
    /// <returns>A JWT token for the newly created manager.</returns>
    [HttpPost("register")]
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
            return BadRequest(new { message = "A user with this nickname already exists." });
        if (await _context.Organizations.AnyAsync(o => o.Name.ToLower() == organizationName.ToLower()))
            return Conflict(new { message = "An organization with this name already exists." });

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var organization = new Organization { Name = organizationName };
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var user = new AppUser(request.Email, nickname) { OrganizationId = organization.Id };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(result.Errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Manager);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(roleResult.Errors);
            }

            await transaction.CommitAsync();
            return Ok(await BuildAuthResponseAsync(user, Roles.Manager));
        }
        catch (DbUpdateException ex) when (IsOrganizationNameUniqueViolation(ex))
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "An organization with this name already exists." });
        }
    }

    /// <summary>Authenticates a user and returns a JWT token.</summary>
    /// <param name="request">Email and password credentials.</param>
    /// <returns>A JWT token with the user's role.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        var claims = await _userManager.GetClaimsAsync(user);
        var mustChangePassword = claims.Any(c =>
            c.Type == AuthClaims.MustChangePassword &&
            c.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

        if (mustChangePassword)
            return StatusCode(StatusCodes.Status403Forbidden,
                new PasswordChangeRequiredResponse("Password change required before first login.", true));

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.StandardUser;
        return Ok(await BuildAuthResponseAsync(user, role));
    }

    /// <summary>Starts forgot password flow by emailing a reset link valid for 30 minutes.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null)
            await TrySendPasswordResetEmailAsync(user);

        return Ok(new ForgotPasswordResponse(ForgotPasswordMessage));
    }

    /// <summary>Completes forgot password flow by setting a new password using a reset token.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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

        return Ok(new { message = "Password has been reset." });
    }

    /// <summary>Changes an initial temporary password and signs in the user.</summary>
    [HttpPost("change-initial-password")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeInitialPassword([FromBody] InitialPasswordChangeRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
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
            return Unauthorized(new { message = "Invalid credentials." });

        var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changeResult.Succeeded)
            return BadRequest(changeResult.Errors);

        if (mustChangePasswordClaims.Count > 0)
            await _userManager.RemoveClaimsAsync(user, mustChangePasswordClaims);

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.StandardUser;
        return Ok(await BuildAuthResponseAsync(user, role));
    }

    /// <summary>Changes the authenticated user's password.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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

        return NoContent();
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

    private async Task<AuthTokenResponse> BuildAuthResponseAsync(AppUser user, string role)
    {
        var organizationName = await GetOrganizationNameAsync(user);
        var token = await GenerateJwtAsync(user, organizationName);
        return new AuthTokenResponse(token, user.Email!, role, user.Nickname, organizationName);
    }

    private async Task<string?> GetOrganizationNameAsync(AppUser user)
    {
        if (!user.OrganizationId.HasValue) return null;

        return await _context.Organizations
            .Where(o => o.Id == user.OrganizationId.Value)
            .Select(o => o.Name)
            .FirstOrDefaultAsync();
    }

    private async Task<string> GenerateJwtAsync(AppUser user, string? organizationName)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.GivenName, user.Nickname),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        if (user.OrganizationId.HasValue)
            claims.Add(new Claim(AuthClaims.OrganizationId, user.OrganizationId.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(organizationName))
            claims.Add(new Claim(AuthClaims.OrganizationName, organizationName));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsOrganizationNameUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
