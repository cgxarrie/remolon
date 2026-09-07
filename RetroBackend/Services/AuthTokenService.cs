using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RetroBackend.Auth;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Models;

namespace RetroBackend.Services;

public class AuthTokenService : IAuthTokenService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RetroDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthTokenService(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthTokenResponse> BuildAuthResponseAsync(AppUser user, string role)
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
}
