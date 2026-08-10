using Microsoft.EntityFrameworkCore;
using RetroBackend.Data;
using RetroBackend.Models;
using RetroBackend.Repositories;
using Xunit;

namespace RetroBackend.Tests.Repositories;

public class EfRetrospectiveRepositoryTests
{
    [Fact]
    public async Task UpdateAsync_PersistsCloseState_ForTrackedRetrospectiveEntity()
    {
        var dbName = $"retro-tests-{Guid.NewGuid()}";

        await using (var context = CreateContext(dbName))
        {
            var repository = new EfRetrospectiveRepository(context);

            var retro = Retrospective.CreateNew("manager-1", "Sprint 42");
            await repository.AddAsync(retro);

            var tracked = await repository.GetByIdAsync(retro.Id);
            Assert.NotNull(tracked);

            tracked!.Close();
            await repository.UpdateAsync(tracked);
        }

        await using (var assertContext = CreateContext(dbName))
        {
            var persisted = await assertContext.Retrospectives.SingleAsync();
            Assert.True(persisted.IsClosed);
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
