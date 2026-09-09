using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RetroBackend.Auth;
using RetroBackend.Controllers;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Models;
using RetroBackend.Services;
using RetroBackend.Tests.Fakes;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class GetRetrospectiveParticipantsTests
{
    [Fact]
    public async Task ManagerFromOtherOrganization_IsForbidden()
    {
        await using var context = CreateContext();
        var (_, _, retro) = await SeedOrgWithAssignedManager(context, "Acme");
        var globex = new Organization { Name = "Globex" };
        var globexManager = new AppUser("boss@globex.test", "globex-boss")
        {
            OrganizationId = globex.Id,
            NormalizedEmail = "BOSS@GLOBEX.TEST",
        };
        var managerRole = await context.Roles.SingleAsync(r => r.NormalizedName == Roles.Manager.ToUpperInvariant());
        context.AddRange(globex, globexManager);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = globexManager.Id, RoleId = managerRole.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = ParticipantsController(context, userManager, globex.Id, globexManager.Id, Roles.Manager);

        var result = await controller.GetRetrospectiveParticipants(retro.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SameOrgManagerNotOnRetrospective_IsForbidden()
    {
        await using var context = CreateContext();
        var (organization, _, retro) = await SeedOrgWithAssignedManager(context, "Acme");
        var outsider = new AppUser("other@acme.test", "other")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "OTHER@ACME.TEST",
        };
        var managerRole = await context.Roles.SingleAsync(r => r.NormalizedName == Roles.Manager.ToUpperInvariant());
        context.Users.Add(outsider);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = outsider.Id, RoleId = managerRole.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = ParticipantsController(context, userManager, organization.Id, outsider.Id, Roles.Manager);

        var result = await controller.GetRetrospectiveParticipants(retro.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AssignedManager_ReturnsParticipants()
    {
        await using var context = CreateContext();
        var (organization, manager, retro) = await SeedOrgWithAssignedManager(context, "Acme");
        using var userManager = CreateUserManager(context);
        var controller = ParticipantsController(context, userManager, organization.Id, manager.Id, Roles.Manager);

        var result = await controller.GetRetrospectiveParticipants(retro.Id);

        var body = Assert.IsAssignableFrom<IEnumerable<UserSummaryDto>>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains(body, user => user.Id == manager.Id);
    }

    [Fact]
    public async Task AssignedStandardUser_ReturnsParticipants()
    {
        await using var context = CreateContext();
        var (organization, manager, retro) = await SeedOrgWithAssignedManager(context, "Acme");
        var participant = new AppUser("user@acme.test", "alice")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "USER@ACME.TEST",
        };
        var standardRole = new IdentityRole(Roles.StandardUser)
        {
            NormalizedName = Roles.StandardUser.ToUpperInvariant(),
        };
        context.AddRange(participant, standardRole);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = participant.Id, RoleId = standardRole.Id });
        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = participant.Id,
            RetrospectiveId = retro.Id,
        });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = ParticipantsController(
            context, userManager, organization.Id, participant.Id, Roles.StandardUser);

        var result = await controller.GetRetrospectiveParticipants(retro.Id);

        var body = Assert.IsAssignableFrom<IEnumerable<UserSummaryDto>>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains(body, user => user.Id == manager.Id);
        Assert.Contains(body, user => user.Id == participant.Id);
    }

    [Fact]
    public async Task UnassignedStandardUser_IsForbidden()
    {
        await using var context = CreateContext();
        var (organization, _, retro) = await SeedOrgWithAssignedManager(context, "Acme");
        var stranger = new AppUser("stranger@acme.test", "stranger")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "STRANGER@ACME.TEST",
        };
        var standardRole = new IdentityRole(Roles.StandardUser)
        {
            NormalizedName = Roles.StandardUser.ToUpperInvariant(),
        };
        context.AddRange(stranger, standardRole);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = stranger.Id, RoleId = standardRole.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = ParticipantsController(
            context, userManager, organization.Id, stranger.Id, Roles.StandardUser);

        var result = await controller.GetRetrospectiveParticipants(retro.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MissingRetrospective_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var (organization, manager, _) = await SeedOrgWithAssignedManager(context, "Acme");
        using var userManager = CreateUserManager(context);
        var controller = ParticipantsController(context, userManager, organization.Id, manager.Id, Roles.Manager);

        var result = await controller.GetRetrospectiveParticipants(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    private static UserAssignmentsController ParticipantsController(
        RetroDbContext context,
        UserManager<AppUser> userManager,
        Guid organizationId,
        string userId,
        string role) =>
        new(context, userManager, new RetroAuthorizationService(context), new NoOpHubMembership())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Role, role),
                        new Claim(AuthClaims.OrganizationId, organizationId.ToString()),
                    ],
                    "test")),
                },
            },
        };

    private static async Task<(Organization Organization, AppUser Manager, Retrospective Retro)>
        SeedOrgWithAssignedManager(RetroDbContext context, string orgName)
    {
        var organization = new Organization { Name = orgName };
        var manager = new AppUser($"manager@{orgName.ToLowerInvariant()}.test", "manager")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = $"MANAGER@{orgName.ToUpperInvariant()}.TEST",
        };
        var managerRole = new IdentityRole(Roles.Manager)
        {
            NormalizedName = Roles.Manager.ToUpperInvariant(),
        };
        var retro = Retrospective.CreateNew(manager.Id, "Sprint", organization.Id);
        context.AddRange(organization, manager, managerRole, retro);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = manager.Id, RoleId = managerRole.Id });
        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = manager.Id,
            RetrospectiveId = retro.Id,
        });
        await context.SaveChangesAsync();
        return (organization, manager, retro);
    }

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"participants-{Guid.NewGuid()}")
            .Options);

    private static UserManager<AppUser> CreateUserManager(RetroDbContext context) =>
        new(
            new UserStore<AppUser>(context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<AppUser>>.Instance);
}
