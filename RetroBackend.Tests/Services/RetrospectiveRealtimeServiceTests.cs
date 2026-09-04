using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RetroBackend.Auth;
using RetroBackend.Data;
using RetroBackend.Models;
using RetroBackend.Repositories;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class RetrospectiveRealtimeServiceTests
{
    [Fact]
    public async Task TryCreateThrow_AssignedUserCanThrow()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var user = Principal(setup.AssignedUserId, Roles.StandardUser, setup.OrganizationId);

        var thrown = await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "axe");

        Assert.NotNull(thrown);
        Assert.Equal(setup.AssignedUserId, thrown!.FromUserId);
        Assert.Equal(setup.TargetUserId, thrown.TargetUserId);
        Assert.Equal("axe", thrown.ObjectId);
    }

    [Fact]
    public async Task TryCreateThrow_OutsiderCannotThrow()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var user = Principal("outsider", Roles.StandardUser, setup.OrganizationId);

        var thrown = await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "axe");

        Assert.Null(thrown);
    }

    [Fact]
    public async Task TryCreateThrow_RejectsInvalidObjectId()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var user = Principal(setup.AssignedUserId, Roles.StandardUser, setup.OrganizationId);

        var thrown = await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "nuke");

        Assert.Null(thrown);
    }

    [Fact]
    public async Task TryCreateThrow_IgnoresThrowAtSelf()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var user = Principal(setup.AssignedUserId, Roles.StandardUser, setup.OrganizationId);

        var thrown = await service.TryCreateThrowAsync(
            user,
            setup.RetrospectiveId,
            setup.AssignedUserId,
            "tomato");

        Assert.Null(thrown);
    }

    [Fact]
    public async Task CanAccess_AdminCanJoinAnyRetrospective()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var admin = Principal("admin", Roles.Admin, Guid.NewGuid());

        Assert.True(await service.CanAccessAsync(admin, setup.RetrospectiveId));
    }

    private static IRetrospectiveRealtimeService CreateService(RetroDbContext context)
    {
        var retrospectiveService = new RetrospectiveService(new EfRetrospectiveRepository(context));
        var authz = new RetroAuthorizationService(context);
        return new RetrospectiveRealtimeService(retrospectiveService, authz);
    }

    private static ClaimsPrincipal Principal(string userId, string role, Guid organizationId)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(AuthClaims.OrganizationId, organizationId.ToString()),
            ],
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    private static async Task<Setup> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"realtime-{Guid.NewGuid()}")
            .Options;
        var context = new RetroDbContext(options);

        var organizationId = Guid.NewGuid();
        context.Organizations.Add(new Organization { Id = organizationId, Name = "Acme" });

        var assigned = new AppUser("alice@acme.test", "Alice")
        {
            Id = "assigned-user",
            OrganizationId = organizationId,
        };
        var target = new AppUser("bob@acme.test", "Bob")
        {
            Id = "target-user",
            OrganizationId = organizationId,
        };
        context.Users.AddRange(assigned, target);

        var retro = Retrospective.CreateNew("manager-1", "Sprint 1", organizationId);
        context.Retrospectives.Add(retro);
        await context.SaveChangesAsync();

        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = assigned.Id,
            RetrospectiveId = retro.Id,
        });
        await context.SaveChangesAsync();

        return new Setup(context, organizationId, retro.Id, assigned.Id, target.Id);
    }

    private sealed record Setup(
        RetroDbContext Context,
        Guid OrganizationId,
        Guid RetrospectiveId,
        string AssignedUserId,
        string TargetUserId);
}
