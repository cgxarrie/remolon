using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RetroBackend.Auth;
using RetroBackend.Data;
using RetroBackend.Hubs;
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
    public async Task TryCreateThrow_RejectsTargetNotOnRetrospective()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var user = Principal(setup.AssignedUserId, Roles.StandardUser, setup.OrganizationId);

        var thrown = await service.TryCreateThrowAsync(user, setup.RetrospectiveId, "stranger", "axe");

        Assert.Null(thrown);
    }

    [Fact]
    public async Task TryCreateThrow_TalliesThrowsReceivedByTheTargetPerObject()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var user = Principal(setup.AssignedUserId, Roles.StandardUser, setup.OrganizationId);

        await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "tomato");
        await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "Tomato");
        await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "axe");

        var tallies = setup.Context.ReceivedThrows
            .Where(t => t.UserId == setup.TargetUserId)
            .ToDictionary(t => t.ObjectId, t => t.Count);
        Assert.Equal(new Dictionary<string, int> { ["tomato"] = 2, ["axe"] = 1 }, tallies);
    }

    [Fact]
    public async Task TryCreateThrow_DoesNotTallyWhenAnyBrowserIsViewingTheRetrospective()
    {
        var setup = await SeedAsync();
        var tracker = new RetrospectiveHubConnectionTracker();
        tracker.Add("watching", setup.TargetUserId);
        tracker.SetRetrospective("watching", setup.RetrospectiveId);
        tracker.Add("elsewhere", setup.TargetUserId);
        var service = CreateService(setup.Context, tracker);
        var user = Principal(setup.AssignedUserId, Roles.StandardUser, setup.OrganizationId);

        var thrown = await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "tomato");

        Assert.NotNull(thrown);
        Assert.Empty(setup.Context.ReceivedThrows);
    }

    [Fact]
    public async Task TryCreateThrow_TalliesWhenTheTargetIsOnlyViewingAnotherRetrospective()
    {
        var setup = await SeedAsync();
        var tracker = new RetrospectiveHubConnectionTracker();
        tracker.Add("other-board", setup.TargetUserId);
        tracker.SetRetrospective("other-board", Guid.NewGuid());
        var service = CreateService(setup.Context, tracker);
        var user = Principal(setup.AssignedUserId, Roles.StandardUser, setup.OrganizationId);

        await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "axe");

        var tally = Assert.Single(setup.Context.ReceivedThrows);
        Assert.Equal("axe", tally.ObjectId);
        Assert.Equal(1, tally.Count);
    }

    [Fact]
    public async Task TryCreateThrow_RejectedThrowIsNotTallied()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var user = Principal(setup.AssignedUserId, Roles.StandardUser, setup.OrganizationId);

        await service.TryCreateThrowAsync(user, setup.RetrospectiveId, setup.TargetUserId, "nuke");

        Assert.Empty(setup.Context.ReceivedThrows);
    }

    [Fact]
    public async Task CanAccess_ManagerFromAnotherOrganizationCannotJoin()
    {
        var setup = await SeedAsync();
        var service = CreateService(setup.Context);
        var manager = Principal("other-manager", Roles.Manager, Guid.NewGuid());

        Assert.False(await service.CanAccessAsync(manager, setup.RetrospectiveId));
    }

    private static IRetrospectiveRealtimeService CreateService(
        RetroDbContext context,
        RetrospectiveHubConnectionTracker? connections = null)
    {
        var retrospectiveService = new RetrospectiveService(new EfRetrospectiveRepository(context));
        var authz = new RetroAuthorizationService(context);
        return new RetrospectiveRealtimeService(
            retrospectiveService,
            authz,
            context,
            connections ?? new RetrospectiveHubConnectionTracker());
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
        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = target.Id,
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
