using Microsoft.EntityFrameworkCore;
using RetroBackend.Data;
using RetroBackend.Models;

namespace RetroBackend.Repositories;

public class EfRetrospectiveRepository : IRetrospectiveRepository
{
    private readonly RetroDbContext _context;

    public EfRetrospectiveRepository(RetroDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Retrospective>> GetAllAsync() =>
        await _context.Retrospectives.Include(r => r.Organization).Include(r => r.Columns).ToListAsync();

    public async Task<IEnumerable<Retrospective>> GetAllForUserAsync(string userId) =>
        await _context.Retrospectives
            .Include(r => r.Organization)
            .Include(r => r.Columns)
            .Where(r => _context.UserRetrospectives.Any(ur => ur.RetrospectiveId == r.Id && ur.UserId == userId))
            .ToListAsync();

    public async Task<Retrospective?> GetByIdAsync(Guid id) =>
        await _context.Retrospectives
            .Include(r => r.Organization)
            .Include(r => r.Columns)
            .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Retrospective> AddAsync(Retrospective retrospective)
    {
        _context.Retrospectives.Add(retrospective);
        await _context.SaveChangesAsync();
        return retrospective;
    }

    public async Task<Retrospective?> UpdateAsync(Retrospective retrospective)
    {
        var exists = await _context.Retrospectives.AnyAsync(r => r.Id == retrospective.Id);
        if (!exists) return null;

        foreach (var column in retrospective.Columns)
        {
            var colExists = await _context.Columns.AnyAsync(c => c.Id == column.Id);
            if (!colExists)
                _context.Columns.Add(column);
            else
                _context.Entry(column).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();
        return retrospective;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var retro = await _context.Retrospectives.FindAsync(id);
        if (retro is null) return false;

        _context.Retrospectives.Remove(retro);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CopyUserAssignmentsAsync(Guid fromRetrospectiveId, Guid toRetrospectiveId)
    {
        var assignments = await _context.UserRetrospectives
            .Where(ur => ur.RetrospectiveId == fromRetrospectiveId)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            _context.UserRetrospectives.Add(new RetroBackend.Models.UserRetrospective
            {
                UserId = assignment.UserId,
                RetrospectiveId = toRetrospectiveId
            });
        }

        await _context.SaveChangesAsync();
    }

    public Task<bool> HasOpenWithTitleAsync(Guid organizationId, string title)
    {
        var normalized = title.ToLower();
        return _context.Retrospectives.AnyAsync(r =>
            r.OrganizationId == organizationId
            && !r.IsClosed
            && r.Title.ToLower() == normalized);
    }
}
