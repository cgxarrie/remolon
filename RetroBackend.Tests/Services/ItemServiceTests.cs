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
            var service = new ItemService(new EfItemRepository(context), new RetroAuthorizationService(context));
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
            var service = new ItemService(new EfItemRepository(context), new RetroAuthorizationService(context));
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

    [Fact]
    public async Task MergeItemsAsync_WhenItemsAreOnDifferentRetrospectives_DoesNotMerge()
    {
        await using var context = CreateContext($"merge-cross-{Guid.NewGuid()}");
        var (sourceId, targetId) = await SeedTwoRetrosWithItemsAsync(context);

        var service = new ItemService(new EfItemRepository(context), new RetroAuthorizationService(context));
        var result = await service.MergeItemsAsync(sourceId, targetId);

        Assert.Null(result);
        Assert.Null((await context.Items.FindAsync(sourceId))!.GroupId);
        Assert.Null((await context.Items.FindAsync(targetId))!.GroupId);
    }

    [Fact]
    public async Task MergeItemsAsync_WhenItemsShareARetrospective_JoinsTheSameGroup()
    {
        await using var context = CreateContext($"merge-same-{Guid.NewGuid()}");
        var organization = new Organization { Name = "Acme" };
        var managerId = "mgr";
        var retro = Retrospective.CreateNew(managerId, "Sprint", organization.Id);
        retro.AddColumn(managerId, "Went Well", 1);
        retro.AddColumn(managerId, "To Improve", 2);
        context.Organizations.Add(organization);
        context.Retrospectives.Add(retro);
        await context.SaveChangesAsync();

        var wentWell = retro.Columns.Single(c => c.Title == "Went Well");
        var toImprove = retro.Columns.Single(c => c.Title == "To Improve");
        var source = new Item(managerId, "M", wentWell.Id, "A", 0);
        var target = new Item(managerId, "M", toImprove.Id, "B", 0);
        context.Items.AddRange(source, target);
        await context.SaveChangesAsync();

        var service = new ItemService(new EfItemRepository(context), new RetroAuthorizationService(context));
        var group = (await service.MergeItemsAsync(source.Id, target.Id))!.ToList();

        Assert.Equal(2, group.Count);
        Assert.NotNull(group[0].GroupId);
        Assert.Equal(group[0].GroupId, group[1].GroupId);
    }

    private static async Task<(Guid SourceId, Guid TargetId)> SeedTwoRetrosWithItemsAsync(RetroDbContext context)
    {
        var organization = new Organization { Name = "Acme" };
        var managerId = "mgr";
        var sourceRetro = Retrospective.CreateNew(managerId, "A", organization.Id);
        var targetRetro = Retrospective.CreateNew(managerId, "B", organization.Id);
        sourceRetro.AddColumn(managerId, "Went Well", 1);
        targetRetro.AddColumn(managerId, "Went Well", 1);
        context.Organizations.Add(organization);
        context.Retrospectives.AddRange(sourceRetro, targetRetro);
        await context.SaveChangesAsync();

        var sourceColumn = sourceRetro.Columns.Single(c => c.Title == "Went Well");
        var targetColumn = targetRetro.Columns.Single(c => c.Title == "Went Well");
        var source = new Item(managerId, "M", sourceColumn.Id, "A", 0);
        var target = new Item(managerId, "M", targetColumn.Id, "B", 0);
        context.Items.AddRange(source, target);
        await context.SaveChangesAsync();
        return (source.Id, target.Id);
    }

    private static RetroDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new RetroDbContext(options);
    }
}
