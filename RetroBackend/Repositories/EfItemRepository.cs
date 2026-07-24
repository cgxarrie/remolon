using Microsoft.EntityFrameworkCore;
using RetroBackend.Data;
using RetroBackend.Models;

namespace RetroBackend.Repositories;

public class EfItemRepository : IItemRepository
{
    private readonly RetroDbContext _context;

    public EfItemRepository(RetroDbContext context)
    {
        _context = context;
    }

    public async Task<Item> AddAsync(Item item)
    {
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<Item?> GetByIdAsync(Guid id) =>
        await _context.Items.FindAsync(id);

    public async Task<IEnumerable<Item>> GetByColumnAsync(Guid columnId) =>
        await _context.Items
            .Where(i => i.ColumnId == columnId && EF.Property<string>(i, "ItemType") == "Item")
            .ToListAsync();

    public async Task<IEnumerable<ActionItem>> GetActionItemsByColumnAsync(Guid columnId) =>
        await _context.Items
            .OfType<ActionItem>()
            .Where(i => i.ColumnId == columnId)
            .ToListAsync();

    public async Task<IEnumerable<Item>> GetByGroupIdAsync(Guid groupId) =>
        await _context.Items
            .Where(i => i.GroupId == groupId)
            .ToListAsync();

    public async Task UpdateRangeAsync(IEnumerable<Item> items)
    {
        _context.Items.UpdateRange(items);
        await _context.SaveChangesAsync();
    }

    public async Task<Item?> UpdateAsync(Item item)
    {
        var exists = await _context.Items.AnyAsync(i => i.Id == item.Id);
        if (!exists) return null;

        _context.Items.Update(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item is null) return false;

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }
}
