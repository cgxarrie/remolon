using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetroBackend.Auth;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Models;

namespace RetroBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin + "," + Roles.Manager)]
public class OrganizationsController : ControllerBase
{
    private readonly RetroDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public OrganizationsController(RetroDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrganizationDto>>> GetAll(int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Organizations.AsNoTracking();

        if (!User.HasRole(Roles.Admin))
        {
            var organizationId = User.FindFirstValue(AuthClaims.OrganizationId);
            if (!Guid.TryParse(organizationId, out var id)) return Forbid();
            query = query.Where(o => o.Id == id);
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(o => o.Name.ToLower())
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationDto(o.Id, o.Name))
            .ToListAsync();
        return Ok(new PagedResponse<OrganizationDto>(items, page, pageSize, totalCount));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<OrganizationDto>> Create(SaveOrganizationRequest request)
    {
        var name = request.Name.Trim();
        if (await _context.Organizations.AnyAsync(o => o.Name.ToLower() == name.ToLower()))
            return Conflict(new { message = "An organization with this name already exists." });

        var organization = new Organization { Name = name };
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { }, new OrganizationDto(organization.Id, organization.Name));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<OrganizationDto>> Update(Guid id, SaveOrganizationRequest request)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization is null) return NotFound();
        var name = request.Name.Trim();
        if (await _context.Organizations.AnyAsync(o => o.Id != id && o.Name.ToLower() == name.ToLower()))
            return Conflict(new { message = "An organization with this name already exists." });

        organization.Name = name;
        await _context.SaveChangesAsync();
        return Ok(new OrganizationDto(organization.Id, organization.Name));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var organization = await _context.Organizations.FindAsync(id);
        if (organization is null) return NotFound();

        var retrospectives = await _context.Retrospectives.Where(r => r.OrganizationId == id).ToListAsync();
        _context.Retrospectives.RemoveRange(retrospectives);
        await _context.SaveChangesAsync();

        var users = await _userManager.Users.Where(u => u.OrganizationId == id).ToListAsync();
        foreach (var user in users)
        {
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);
        }

        _context.Organizations.Remove(organization);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
