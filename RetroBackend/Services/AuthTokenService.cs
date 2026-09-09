using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RetroBackend.Auth;
using RetroBackend.Config;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Models;

namespace RetroBackend.Services;

public class AuthTokenService : IAuthTokenService
{
    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);

    private readonly UserManager<AppUser> _userManager;
    private readonly RetroDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly string _signingKey;

    public AuthTokenService(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _signingKey = JwtSigningKey.Resolve(configuration);
    }

    public async Task<AuthTokenResponse> BuildAuthResponseAsync(AppUser user, string role)
    {
        var organizationName = await GetOrganizationNameAsync(user);
        var token = await GenerateJwtAsync(user, organizationName);
        var refreshToken = await IssueRefreshTokenAsync(user.Id);
        return new AuthTokenResponse(token, user.Email!, role, user.Nickname, organizationName, refreshToken);
    }

    public async Task<AuthTokenResponse?> RefreshAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTime.UtcNow || stored.User is null)
            return null;

        stored.RevokedAt = DateTime.UtcNow;
        var roles = await _userManager.GetRolesAsync(stored.User);
        var role = roles.FirstOrDefault() ?? Roles.StandardUser;
        return await BuildAuthResponseAsync(stored.User, role);
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();
        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (stored is null || stored.RevokedAt is not null) return;
        stored.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private async Task<string> IssueRefreshTokenAsync(string userId)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(raw),
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
        });
        await _context.SaveChangesAsync();
        return raw;
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
        var securityStamp = await _userManager.GetSecurityStampAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.GivenName, user.Nickname),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AuthClaims.SecurityStamp, securityStamp),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        if (user.OrganizationId.HasValue)
            claims.Add(new Claim(AuthClaims.OrganizationId, user.OrganizationId.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(organizationName))
            claims.Add(new Claim(AuthClaims.OrganizationName, organizationName));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(AccessTokenLifetime),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
