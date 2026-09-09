using Microsoft.EntityFrameworkCore;
using RetroBackend.Data;
using RetroBackend.Models;
using RetroBackend.Repositories;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class RetrospectiveServiceTests
{
    [Fact]
    public async Task CloseAsync_CreatesNextIterationAndCopiesActionItemsAsPending()
    {
        var dbName = $"retro-service-tests-{Guid.NewGuid()}";
        var ownerUserId = "manager-1";
        var organizationId = Guid.NewGuid();
        Guid originalRetroId;

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Organizations.Add(new Organization { Id = organizationId, Name = "Acme" });
            var original = Retrospective.CreateNew(ownerUserId, "Sprint 11", organizationId);
            original.AddColumn(ownerUserId, "Went Well", 1);
            original.Reveal();

            var pending = original.Columns.OfType<ActionColumn>().Single(c => c.Title == "Pending Action Items");
            pending.Items.Add(new ActionItem(ownerUserId, "Mgr", ["Alice", "Dave"], pending.Id, "Finish docs", 0, 2));
            var completedPending = new ActionItem(ownerUserId, "Mgr", ["Bob"], pending.Id, "Already done", 1, 1);
            completedPending.Complete(ownerUserId);
            pending.Items.Add(completedPending);

            var actions = original.Columns.OfType<ActionColumn>().Single(c => c.Title == "Action Items");
            actions.Items.Add(new ActionItem(ownerUserId, "Mgr", ["Carol"], actions.Id, "Ship feature", 0));

            seedContext.Retrospectives.Add(original);
            await seedContext.SaveChangesAsync();
            originalRetroId = original.Id;
        }

        Guid nextId;
        await using (var actionContext = CreateContext(dbName))
        {
            var service = new RetrospectiveService(new EfRetrospectiveRepository(actionContext));
            var next = await service.CloseAsync(originalRetroId, new CloseRetrospectiveRequest
            {
                CurrentUser = "manager-2",
            });

            Assert.NotNull(next);
            Assert.NotEqual(originalRetroId, next.Id);
            Assert.False(next.IsClosed);
            Assert.Equal("Sprint 11", next.Title);
            nextId = next.Id;
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var original = await assertContext.Retrospectives.SingleAsync(r => r.Id == originalRetroId);
            Assert.True(original.IsClosed);

            var next = await assertContext.Retrospectives
                .Include(r => r.Columns)
                .ThenInclude(c => c.Items)
                .SingleAsync(r => r.Id == nextId);

            Assert.Contains(next.Columns, c => c.Title == "Went Well" && c is not ActionColumn);

            var pending = next.Columns.OfType<ActionColumn>().Single(c => c.Title == "Pending Action Items");
            var copied = pending.Items.OfType<ActionItem>().OrderBy(i => i.Position).ToList();
            Assert.Equal(2, copied.Count);
            Assert.Equal("Finish docs", copied[0].Description);
            Assert.Equal(["Alice", "Dave"], copied[0].Assignees);
            Assert.Equal(3, copied[0].Iterations);
            Assert.False(copied[0].IsCompleted);
            Assert.Equal("Ship feature", copied[1].Description);
            Assert.Equal(["Carol"], copied[1].Assignees);
            Assert.Equal(1, copied[1].Iterations);
            Assert.DoesNotContain(copied, i => i.Description == "Already done");
        }
    }

    [Fact]
    public async Task CloseAsync_WhenUnrevealed_ReturnsNullAndLeavesRetroOpen()
    {
        var dbName = $"retro-service-unrevealed-close-{Guid.NewGuid()}";
        var organizationId = Guid.NewGuid();
        Guid retroId;

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Organizations.Add(new Organization { Id = organizationId, Name = "Acme" });
            var original = Retrospective.CreateNew("manager-1", "Sprint 11", organizationId);
            seedContext.Retrospectives.Add(original);
            await seedContext.SaveChangesAsync();
            retroId = original.Id;
        }

        await using (var actionContext = CreateContext(dbName))
        {
            var service = new RetrospectiveService(new EfRetrospectiveRepository(actionContext));
            var created = await service.CloseAsync(retroId, new CloseRetrospectiveRequest { CurrentUser = "manager-1" });
            Assert.Null(created);
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var retro = await assertContext.Retrospectives.SingleAsync();
            Assert.False(retro.IsClosed);
            Assert.False(retro.IsRevealed);
        }
    }

    [Fact]
    public async Task RevealAsync_SetsRetrospectiveDateToNow()
    {
        var dbName = $"retro-service-reveal-date-{Guid.NewGuid()}";
        var organizationId = Guid.NewGuid();
        Guid retroId;
        var before = DateTime.UtcNow.AddSeconds(-1);

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Organizations.Add(new Organization { Id = organizationId, Name = "Acme" });
            var original = Retrospective.CreateNew("manager-1", "Sprint 11", organizationId);
            seedContext.Retrospectives.Add(original);
            await seedContext.SaveChangesAsync();
            retroId = original.Id;
        }

        await using (var actionContext = CreateContext(dbName))
        {
            var service = new RetrospectiveService(new EfRetrospectiveRepository(actionContext));
            var revealed = await service.RevealAsync(retroId);
            Assert.NotNull(revealed);
            Assert.True(revealed!.IsRevealed);
            Assert.NotNull(revealed.RetrospectiveDate);
            Assert.InRange(revealed.RetrospectiveDate!.Value, before, DateTime.UtcNow.AddSeconds(1));
        }
    }

    [Fact]
    public async Task CreateAsync_StampsOrganizationId()
    {
        var dbName = $"retro-service-create-tests-{Guid.NewGuid()}";
        var organizationId = Guid.NewGuid();
        await using var context = CreateContext(dbName);
        context.Organizations.Add(new Organization { Id = organizationId, Name = "Globex" });
        await context.SaveChangesAsync();
        var service = new RetrospectiveService(new EfRetrospectiveRepository(context));

        var created = await service.CreateAsync(new CreateRetrospectiveRequest
        {
            CurrentUser = "manager-2",
            OrganizationId = organizationId,
            Title = "Sprint 12",
        });

        Assert.Equal(organizationId, created.OrganizationId);
    }

    [Fact]
    public async Task CreateNextIterationAsync_CopiesPendingAndActionItemsAsPending()
    {
        var dbName = $"retro-next-iteration-{Guid.NewGuid()}";
        var organizationId = Guid.NewGuid();
        Guid closedId;

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Organizations.Add(new Organization { Id = organizationId, Name = "Acme" });
            var closed = Retrospective.CreateNew("manager-1", "Sprint 11", organizationId);
            closed.AddColumn("manager-1", "Went Well", 1);
            closed.Reveal();
            closed.Close();
            var pending = closed.Columns.OfType<ActionColumn>().Single(c => c.Title == "Pending Action Items");
            pending.Items.Add(new ActionItem("manager-1", "Mgr", ["Alice", "Eve"], pending.Id, "Carry this", 0, 2));
            var actions = closed.Columns.OfType<ActionColumn>().Single(c => c.Title == "Action Items");
            actions.Items.Add(new ActionItem("manager-1", "Mgr", ["Bob"], actions.Id, "New action", 0));
            seedContext.Retrospectives.Add(closed);
            await seedContext.SaveChangesAsync();
            closedId = closed.Id;
        }

        Guid nextId;
        await using (var actionContext = CreateContext(dbName))
        {
            var service = new RetrospectiveService(new EfRetrospectiveRepository(actionContext));
            var next = await service.CreateNextIterationAsync(closedId, "manager-2");
            Assert.NotNull(next);
            nextId = next.Id;
            Assert.False(next.IsClosed);
            Assert.Equal("Sprint 11", next.Title);
            Assert.Contains(next.Columns, c => c.Title == "Went Well" && c is not ActionColumn);
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var next = await assertContext.Retrospectives
                .Include(r => r.Columns)
                .ThenInclude(c => c.Items)
                .SingleAsync(r => r.Id == nextId);
            var pending = next.Columns.OfType<ActionColumn>().Single(c => c.Title == "Pending Action Items");
            var copied = pending.Items.OfType<ActionItem>().OrderBy(i => i.Position).ToList();
            Assert.Equal(2, copied.Count);
            Assert.Equal("Carry this", copied[0].Description);
            Assert.Equal(["Alice", "Eve"], copied[0].Assignees);
            Assert.Equal(3, copied[0].Iterations);
            Assert.Equal("New action", copied[1].Description);
            Assert.Equal(["Bob"], copied[1].Assignees);
            Assert.Equal(1, copied[1].Iterations);
        }
    }

    [Fact]
    public async Task CreateNextIterationAsync_WhenOpenExists_ReturnsNull()
    {
        var dbName = $"retro-next-open-{Guid.NewGuid()}";
        var organizationId = Guid.NewGuid();
        Guid closedId;

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Organizations.Add(new Organization { Id = organizationId, Name = "Acme" });
            var closed = Retrospective.CreateNew("manager-1", "Sprint 11", organizationId);
            closed.Reveal();
            closed.Close();
            var open = Retrospective.CreateNew("manager-1", "Sprint 11", organizationId);
            seedContext.Retrospectives.AddRange(closed, open);
            await seedContext.SaveChangesAsync();
            closedId = closed.Id;
        }

        await using var actionContext = CreateContext(dbName);
        var service = new RetrospectiveService(new EfRetrospectiveRepository(actionContext));
        Assert.Null(await service.CreateNextIterationAsync(closedId, "manager-1"));
    }

    private static RetroDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new RetroDbContext(options);
    }
}
