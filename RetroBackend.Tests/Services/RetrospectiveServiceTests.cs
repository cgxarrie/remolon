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
    public async Task CloseAsync_ClosesExistingAndCreatesNextRetrospective_WhenCurrentUserIsEmpty()
    {
        var dbName = $"retro-service-tests-{Guid.NewGuid()}";
        var ownerUserId = "manager-1";
        var organizationId = Guid.NewGuid();
        Guid originalRetroId;

        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Organizations.Add(new Organization { Id = organizationId, Name = "Acme" });
            var original = Retrospective.CreateNew(ownerUserId, "Sprint 11", organizationId);
            original.Reveal();
            seedContext.Retrospectives.Add(original);
            await seedContext.SaveChangesAsync();
            originalRetroId = original.Id;
        }

        Guid newRetroId;
        await using (var actionContext = CreateContext(dbName))
        {
            var repository = new EfRetrospectiveRepository(actionContext);
            var service = new RetrospectiveService(repository);

            var created = await service.CloseAsync(originalRetroId, new CloseRetrospectiveRequest
            {
                CurrentUser = string.Empty,
            });

            Assert.NotNull(created);
            newRetroId = created.Id;
            Assert.Equal(ownerUserId, created.CreatedBy);
            Assert.Equal(organizationId, created.OrganizationId);
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var allRetros = await assertContext.Retrospectives.ToListAsync();
            Assert.Equal(2, allRetros.Count);

            var oldRetro = allRetros.Single(r => r.Id == originalRetroId);
            var nextRetro = allRetros.Single(r => r.Id == newRetroId);

            Assert.True(oldRetro.IsClosed);
            Assert.Equal(ownerUserId, nextRetro.CreatedBy);
            Assert.False(nextRetro.IsClosed);
            Assert.Equal(organizationId, nextRetro.OrganizationId);
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

    private static RetroDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new RetroDbContext(options);
    }
}
