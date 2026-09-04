using Microsoft.EntityFrameworkCore;
using RetroBackend.Data;

namespace RetroBackend.Services;

public class RetroAuthorizationService : IRetroAuthorizationService
{
    private readonly RetroDbContext _context;

    public RetroAuthorizationService(RetroDbContext context)
    {
        _context = context;
    }

    public Task<bool> IsAssignedToRetrospectiveAsync(string userId, Guid retrospectiveId) =>
        _context.UserRetrospectives
            .AnyAsync(ur => ur.UserId == userId && ur.RetrospectiveId == retrospectiveId);

    public Task<Guid?> GetRetrospectiveIdByColumnAsync(Guid columnId) =>
        _context.Columns
            .Where(c => c.Id == columnId)
            .Select(c => (Guid?)c.RetrospectiveId)
            .FirstOrDefaultAsync();

    public async Task<bool> IsAssignedToRetrospectiveByColumnAsync(string userId, Guid columnId)
    {
        var retroId = await GetRetrospectiveIdByColumnAsync(columnId);
        if (retroId is null) return false;

        return await IsAssignedToRetrospectiveAsync(userId, retroId.Value);
    }

    public async Task<bool> IsItemOwnerAsync(string userId, Guid itemId)
    {
        var item = await _context.Items.FindAsync(itemId);
        return item?.CreatedBy == userId;
    }

    public async Task<bool> IsAssignedToRetrospectiveByItemAsync(string userId, Guid itemId)
    {
        var columnId = await _context.Items
            .Where(i => i.Id == itemId)
            .Select(i => (Guid?)i.ColumnId)
            .FirstOrDefaultAsync();

        if (columnId is null) return false;
        return await IsAssignedToRetrospectiveByColumnAsync(userId, columnId.Value);
    }

    public Task<bool> IsRetrospectiveOwnerAsync(string userId, Guid retrospectiveId) =>
        _context.Retrospectives.AnyAsync(r => r.Id == retrospectiveId && r.CreatedBy == userId);

    public async Task<bool> IsRetrospectiveOwnerByColumnAsync(string userId, Guid columnId)
    {
        var retroId = await GetRetrospectiveIdByColumnAsync(columnId);
        if (retroId is null) return false;
        return await IsRetrospectiveOwnerAsync(userId, retroId.Value);
    }

    public async Task<bool> IsRetrospectiveOwnerByItemAsync(string userId, Guid itemId)
    {
        var columnId = await _context.Items
            .Where(i => i.Id == itemId)
            .Select(i => (Guid?)i.ColumnId)
            .FirstOrDefaultAsync();

        if (columnId is null) return false;
        return await IsRetrospectiveOwnerByColumnAsync(userId, columnId.Value);
    }

    public async Task<bool> IsRetrospectiveRevealedByItemAsync(Guid itemId)
    {
        var revealed = await (
            from item in _context.Items
            join column in _context.Columns on item.ColumnId equals column.Id
            join retro in _context.Retrospectives on column.RetrospectiveId equals retro.Id
            where item.Id == itemId
            select (bool?)retro.IsRevealed
        ).FirstOrDefaultAsync();

        return revealed == true;
    }

    public async Task<Guid?> GetRetrospectiveIdByItemAsync(Guid itemId)
    {
        var columnId = await _context.Items
            .Where(i => i.Id == itemId)
            .Select(i => (Guid?)i.ColumnId)
            .FirstOrDefaultAsync();

        if (columnId is null) return null;
        return await GetRetrospectiveIdByColumnAsync(columnId.Value);
    }
}
