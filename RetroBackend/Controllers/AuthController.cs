using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using RetroBackend.Dtos;
using RetroBackend.Auth;
using RetroBackend.Models;
using RetroBackend.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly RetroDbContext _context;

    public AuthController(UserManager<AppUser> userManager, IConfiguration configuration, RetroDbContext context)
    {
        _userManager = userManager;
        _configuration = configuration;
        _context = context;
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

    /// <summary>Starts forgot password flow and returns a reset token for the provided email.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Ok(new ForgotPasswordResponse("If the account exists, reset instructions were generated.", null));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return Ok(new ForgotPasswordResponse(
            "Reset token generated. Use it to set a new password.",
            encodedToken
        ));
    }

    /// <summary>Completes forgot password flow by setting a new password using a reset token.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return BadRequest(new { message = "Invalid reset request." });

        var tokenInput = request.Token?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tokenInput))
            return BadRequest(new { message = "Reset token is required." });

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(tokenInput));
        }
        catch
        {
            // Backward compatibility: allow clients that submit the raw token directly.
            decodedToken = tokenInput;
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

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

    /// <summary>Registers a new super user. Requires an existing super user.</summary>
    /// <param name="request">Email and password for the new super user account.</param>
    /// <returns>Confirmation of the created super user.</returns>
    [HttpPost("register-superuser")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterSuperUser([FromBody] RegisterRequest request)
    {
        var user = new AppUser(request.Email, request.Nickname);
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, Roles.Admin);
        return Ok(new { message = $"SuperUser '{user.Email}' created." });
    }

    /// <summary>Registers a new manager. Requires an existing admin.</summary>
    /// <param name="request">Email and password for the new manager account.</param>
    /// <returns>Confirmation of the created manager.</returns>
    [HttpPost("register-manager")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterManager([FromBody] RegisterRequest request)
    {
        if (!request.OrganizationId.HasValue
            || !await _context.Organizations.AnyAsync(o => o.Id == request.OrganizationId))
            return BadRequest(new { message = "A valid organizationId is required." });
        if (await _userManager.Users.AnyAsync(u => u.Nickname.ToLower() == request.Nickname.ToLower()))
            return BadRequest(new { message = "A user with this nickname already exists." });

        var user = new AppUser(request.Email, request.Nickname) { OrganizationId = request.OrganizationId };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, Roles.Manager);
        return Ok(new { message = $"Manager '{user.Email}' created." });
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
