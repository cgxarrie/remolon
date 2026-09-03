using Microsoft.EntityFrameworkCore;
using RetroBackend.Data;
using RetroBackend.Models;
using Xunit;

namespace RetroBackend.Tests.Data;

public class MultiTenancyTests
{
    [Fact]
    public async Task OrganizationScopedQuery_ReturnsCorrectPageTotalAndNicknameOrder()
    {
        await using var context = CreateContext();
        var acme = new Organization { Name = "Acme" };
        var globex = new Organization { Name = "Globex" };
        context.Organizations.AddRange(acme, globex);
        context.Users.AddRange(
            new AppUser("zoe@acme.test", "zoe") { OrganizationId = acme.Id },
            new AppUser("alice@acme.test", "Alice") { OrganizationId = acme.Id },
            new AppUser("bob@acme.test", "bob") { OrganizationId = acme.Id },
            new AppUser("alice@globex.test", "alice.globex") { OrganizationId = globex.Id });
        await context.SaveChangesAsync();

        var query = context.Users.Where(u => u.OrganizationId == acme.Id);
        var totalCount = await query.CountAsync();
        var page = await query.OrderBy(u => u.Nickname.ToLower()).ThenBy(u => u.Email)
            .Skip(1).Take(2).Select(u => u.Nickname).ToListAsync();

        Assert.Equal(3, totalCount);
        Assert.Equal(["bob", "zoe"], page);
    }

    [Fact]
    public void Model_HasUniqueNicknameAndOrganizationNameIndexesAndCascadeForeignKeys()
    {
        using var context = CreateContext();
        var user = context.Model.FindEntityType(typeof(AppUser))!;
        var organization = context.Model.FindEntityType(typeof(Organization))!;
        var retrospective = context.Model.FindEntityType(typeof(Retrospective))!;

        Assert.True(user.GetIndexes().Single(i => i.Properties.Single().Name == nameof(AppUser.Nickname)).IsUnique);
        Assert.True(organization.GetIndexes().Single(i => i.Properties.Single().Name == nameof(Organization.Name)).IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, user.GetForeignKeys().Single(f => f.PrincipalEntityType.ClrType == typeof(Organization)).DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, retrospective.GetForeignKeys().Single(f => f.PrincipalEntityType.ClrType == typeof(Organization)).DeleteBehavior);
    }

    private static RetroDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"multi-tenancy-{Guid.NewGuid()}")
            .Options;
        return new RetroDbContext(options);
    }
}
