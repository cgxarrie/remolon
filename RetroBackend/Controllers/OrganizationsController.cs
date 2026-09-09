using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using RetroBackend.Auth;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Models;
using RetroBackend.Services;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Manager + "," + Roles.StandardUser)]
public class OrganizationsController : ControllerBase
{
    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    private readonly RetroDbContext _context;

    public OrganizationsController(RetroDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrganizationDto>>> GetAll(int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Organizations.AsNoTracking();

        var organizationId = User.FindFirstValue(AuthClaims.OrganizationId);
        if (!Guid.TryParse(organizationId, out var id)) return Forbid();
        query = query.Where(o => o.Id == id);

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(o => o.Name.ToLower())
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationDto(
                o.Id,
                o.Name,
                new OrganizationThemeDto(
                    o.ThemeKey,
                    o.ThemeHeaderColor,
                    o.ThemeHeaderHoverColor,
                    o.ThemeAccentColor,
                    o.ThemeAccentHoverColor,
                    o.ThemeFocusColor)))
            .ToListAsync();
        return Ok(new PagedResponse<OrganizationDto>(items, page, pageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationDto>> GetById(Guid id)
    {
        var organizationId = User.FindFirstValue(AuthClaims.OrganizationId);
        if (!Guid.TryParse(organizationId, out var callerOrganizationId) || callerOrganizationId != id)
            return Forbid();

        var organization = await _context.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return organization is null ? NotFound() : Ok(ToDto(organization));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<ActionResult<OrganizationDto>> Update(Guid id, SaveOrganizationRequest request)
    {
        var organizationId = User.FindFirstValue(AuthClaims.OrganizationId);
        if (!Guid.TryParse(organizationId, out var callerOrganizationId) || callerOrganizationId != id)
            return Forbid();

        var organization = await _context.Organizations.FindAsync(id);
        if (organization is null) return NotFound();
        var name = request.Name.Trim();
        if (await _context.Organizations.AnyAsync(o => o.Id != id && o.Name.ToLower() == name.ToLower()))
            return Conflict(new { message = "An organization with this name already exists." });

        var themeError = ValidateTheme(request);
        if (themeError is not null) return BadRequest(new { message = themeError });

        organization.Name = name;
        ApplyTheme(organization, request);
        await _context.SaveChangesAsync();
        return Ok(ToDto(organization));
    }

    private static OrganizationDto ToDto(Organization organization) =>
        new(
            organization.Id,
            organization.Name,
            new OrganizationThemeDto(
                organization.ThemeKey,
                organization.ThemeHeaderColor,
                organization.ThemeHeaderHoverColor,
                organization.ThemeAccentColor,
                organization.ThemeAccentHoverColor,
                organization.ThemeFocusColor));

    private static string? ValidateTheme(SaveOrganizationRequest request)
    {
        var themeKey = request.ThemeKey.Trim().ToLowerInvariant();
        if (!OrganizationThemes.IsKnown(themeKey))
            return $"ThemeKey must be '{OrganizationThemes.Custom}' or one of the preset themes.";

        if (themeKey != OrganizationThemes.Custom) return null;

        var colors = new[]
        {
            request.HeaderColor,
            request.HeaderHoverColor,
            request.AccentColor,
            request.AccentHoverColor,
            request.FocusColor,
        };
        return colors.All(color => color is not null && HexColor.IsMatch(color))
            ? null
            : "Custom theme colors are required and must use #RRGGBB format.";
    }

    private static void ApplyTheme(Organization organization, SaveOrganizationRequest request)
    {
        organization.ThemeKey = request.ThemeKey.Trim().ToLowerInvariant();
        var isCustom = organization.ThemeKey == OrganizationThemes.Custom;
        organization.ThemeHeaderColor = isCustom ? request.HeaderColor : null;
        organization.ThemeHeaderHoverColor = isCustom ? request.HeaderHoverColor : null;
        organization.ThemeAccentColor = isCustom ? request.AccentColor : null;
        organization.ThemeAccentHoverColor = isCustom ? request.AccentHoverColor : null;
        organization.ThemeFocusColor = isCustom ? request.FocusColor : null;
    }
}
