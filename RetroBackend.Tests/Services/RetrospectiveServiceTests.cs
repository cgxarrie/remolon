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
        Guid originalRetroId;

        await using (var seedContext = CreateContext(dbName))
        {
            var original = Retrospective.CreateNew(ownerUserId, "Sprint 11");
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
