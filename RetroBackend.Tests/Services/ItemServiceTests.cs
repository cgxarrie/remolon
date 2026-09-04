using Microsoft.EntityFrameworkCore;
using RetroBackend.Data;
using RetroBackend.Models;
using RetroBackend.Repositories;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class ItemServiceTests
{
    [Fact]
    public async Task UnlinkFromGroupAsync_WhenOneItemRemains_ClearsThatItemGroup()
    {
        var dbName = $"item-unlink-{Guid.NewGuid()}";
        var groupId = Guid.NewGuid();
        Guid firstId;
        Guid secondId;

        await using (var context = CreateContext(dbName))
        {
            var columnId = Guid.NewGuid();
            var first = new Item("alice", "Alice", columnId, "First", 0);
            var second = new Item("bob", "Bob", columnId, "Second", 1);
            first.JoinGroup(groupId);
            second.JoinGroup(groupId);
            context.Items.AddRange(first, second);
            await context.SaveChangesAsync();
            firstId = first.Id;
            secondId = second.Id;
        }

        await using (var context = CreateContext(dbName))
        {
            var service = new ItemService(new EfItemRepository(context));
            var unlinked = await service.UnlinkFromGroupAsync(firstId);
            Assert.NotNull(unlinked);
            Assert.Null(unlinked!.GroupId);
        }

        await using (var context = CreateContext(dbName))
        {
            var remaining = await context.Items.SingleAsync(i => i.Id == secondId);
            Assert.Null(remaining.GroupId);
        }
    }

    [Fact]
    public async Task UnlinkFromGroupAsync_WhenTwoItemsRemain_KeepsTheirGroup()
    {
        var dbName = $"item-unlink-keep-{Guid.NewGuid()}";
        var groupId = Guid.NewGuid();
        Guid firstId;
        Guid secondId;
        Guid thirdId;

        await using (var context = CreateContext(dbName))
        {
            var columnId = Guid.NewGuid();
            var first = new Item("alice", "Alice", columnId, "First", 0);
            var second = new Item("bob", "Bob", columnId, "Second", 1);
            var third = new Item("cara", "Cara", columnId, "Third", 2);
            first.JoinGroup(groupId);
            second.JoinGroup(groupId);
            third.JoinGroup(groupId);
            context.Items.AddRange(first, second, third);
            await context.SaveChangesAsync();
            firstId = first.Id;
            secondId = second.Id;
            thirdId = third.Id;
        }

        await using (var context = CreateContext(dbName))
        {
            var service = new ItemService(new EfItemRepository(context));
            await service.UnlinkFromGroupAsync(firstId);
        }

        await using (var context = CreateContext(dbName))
        {
            var second = await context.Items.SingleAsync(i => i.Id == secondId);
            var third = await context.Items.SingleAsync(i => i.Id == thirdId);
            Assert.Equal(groupId, second.GroupId);
            Assert.Equal(groupId, third.GroupId);
        }
    }

    private static RetroDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new RetroDbContext(options);
    }
}
