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
using RetroBackend.Repositories;
using RetroBackend.Services;
using RetroBackend.Tests.Fakes;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class RetrospectiveManagerRulesTests
{
    [Fact]
    public async Task Create_WithoutManager_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        var service = new RetrospectiveService(new EfRetrospectiveRepository(context));
        var controller = new RetrospectivesController(
            service,
            new RetroAuthorizationService(context),
            context,
            new RecordingLiveNotifier(),
            new NoOpClosedRetrospectiveActionItemMailer())
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id),
        };

        var result = await controller.Create(new RetroBackend.Dtos.CreateRetrospectiveRequest
        {
            Title = "Sprint",
            OrganizationId = organization.Id,
            ManagerUserIds = [],
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Retrospectives);
    }

    [Fact]
    public async Task UnassignUser_LastManager_ReturnsBadRequestAndKeepsAssignment()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var manager = new AppUser("manager@acme.test", "manager")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "MANAGER@ACME.TEST",
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
        using var userManager = CreateUserManager(context);
        var controller = new UserAssignmentsController(context, userManager, new RetroAuthorizationService(context), new NoOpHubMembership())
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id, manager.Id),
        };

        var result = await controller.UnassignUser(new AssignUserRequest(manager.Email!, retro.Id));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(await context.UserRetrospectives.AnyAsync(
            assignment => assignment.UserId == manager.Id && assignment.RetrospectiveId == retro.Id));
    }

    [Fact]
    public async Task AssignUser_ManagerNotOnRetrospective_CannotSelfAssign()
    {
        await using var context = CreateContext();
        var (organization, _, outsider, retro) = await SeedTwoManagersAndRetro(context);
        using var userManager = CreateUserManager(context);
        var controller = AssignmentsController(context, userManager, organization.Id, outsider.Id);

        var result = await controller.AssignUser(new AssignUserRequest(outsider.Email!, retro.Id));

        Assert.IsType<ForbidResult>(result);
        Assert.False(await context.UserRetrospectives.AnyAsync(
            assignment => assignment.UserId == outsider.Id && assignment.RetrospectiveId == retro.Id));
    }

    [Fact]
    public async Task AssignUsers_ManagerNotOnRetrospective_CannotSelfAssign()
    {
        await using var context = CreateContext();
        var (organization, _, outsider, retro) = await SeedTwoManagersAndRetro(context);
        using var userManager = CreateUserManager(context);
        var controller = AssignmentsController(context, userManager, organization.Id, outsider.Id);

        var result = await controller.AssignUsers(new BatchAssignUsersRequest(retro.Id, [outsider.Id]));

        Assert.IsType<ForbidResult>(result);
        Assert.False(await context.UserRetrospectives.AnyAsync(
            assignment => assignment.UserId == outsider.Id && assignment.RetrospectiveId == retro.Id));
    }

    [Fact]
    public async Task AssignUser_AssignedManager_CanAssignOrgUser()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var manager = new AppUser("manager@acme.test", "manager")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "MANAGER@ACME.TEST",
        };
        var participant = new AppUser("user@acme.test", "user")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "USER@ACME.TEST",
        };
        var managerRole = new IdentityRole(Roles.Manager)
        {
            NormalizedName = Roles.Manager.ToUpperInvariant(),
        };
        var retro = Retrospective.CreateNew(manager.Id, "Sprint", organization.Id);
        context.AddRange(organization, manager, participant, managerRole, retro);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = manager.Id, RoleId = managerRole.Id });
        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = manager.Id,
            RetrospectiveId = retro.Id,
        });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = AssignmentsController(context, userManager, organization.Id, manager.Id);

        var result = await controller.AssignUser(new AssignUserRequest(participant.Email!, retro.Id));

        Assert.IsType<OkObjectResult>(result);
        Assert.True(await context.UserRetrospectives.AnyAsync(
            assignment => assignment.UserId == participant.Id && assignment.RetrospectiveId == retro.Id));
    }

    [Fact]
    public async Task UnassignUser_NonOwner_RemovesHubMembership()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var manager = new AppUser("manager@acme.test", "manager")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "MANAGER@ACME.TEST",
        };
        var participant = new AppUser("user@acme.test", "user")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "USER@ACME.TEST",
        };
        var managerRole = new IdentityRole(Roles.Manager)
        {
            NormalizedName = Roles.Manager.ToUpperInvariant(),
        };
        var retro = Retrospective.CreateNew(manager.Id, "Sprint", organization.Id);
        context.AddRange(organization, manager, participant, managerRole, retro);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = manager.Id, RoleId = managerRole.Id });
        context.UserRetrospectives.AddRange(
            new UserRetrospective { UserId = manager.Id, RetrospectiveId = retro.Id },
            new UserRetrospective { UserId = participant.Id, RetrospectiveId = retro.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var membership = new RecordingHubMembership();
        var controller = new UserAssignmentsController(
            context,
            userManager,
            new RetroAuthorizationService(context),
            membership)
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id, manager.Id),
        };

        var result = await controller.UnassignUser(new AssignUserRequest(participant.Email!, retro.Id));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal([(participant.Id, retro.Id)], membership.Removals);
    }

    [Fact]
    public async Task UnassignUser_Owner_KeepsHubMembership()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var owner = new AppUser("owner@acme.test", "owner")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "OWNER@ACME.TEST",
        };
        var manager = new AppUser("manager@acme.test", "manager")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "MANAGER@ACME.TEST",
        };
        var managerRole = new IdentityRole(Roles.Manager)
        {
            NormalizedName = Roles.Manager.ToUpperInvariant(),
        };
        var retro = Retrospective.CreateNew(owner.Id, "Sprint", organization.Id);
        context.AddRange(organization, owner, manager, managerRole, retro);
        context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = owner.Id, RoleId = managerRole.Id },
            new IdentityUserRole<string> { UserId = manager.Id, RoleId = managerRole.Id });
        context.UserRetrospectives.AddRange(
            new UserRetrospective { UserId = owner.Id, RetrospectiveId = retro.Id },
            new UserRetrospective { UserId = manager.Id, RetrospectiveId = retro.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var membership = new RecordingHubMembership();
        var controller = new UserAssignmentsController(
            context,
            userManager,
            new RetroAuthorizationService(context),
            membership)
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id, manager.Id),
        };

        var result = await controller.UnassignUser(new AssignUserRequest(owner.Email!, retro.Id));

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(membership.Removals);
    }

    [Fact]
    public async Task AssignUsers_RemovedParticipant_RemovesHubMembership()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var manager = new AppUser("manager@acme.test", "manager")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "MANAGER@ACME.TEST",
        };
        var participant = new AppUser("user@acme.test", "user")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "USER@ACME.TEST",
        };
        var managerRole = new IdentityRole(Roles.Manager)
        {
            NormalizedName = Roles.Manager.ToUpperInvariant(),
        };
        var retro = Retrospective.CreateNew(manager.Id, "Sprint", organization.Id);
        context.AddRange(organization, manager, participant, managerRole, retro);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = manager.Id, RoleId = managerRole.Id });
        context.UserRetrospectives.AddRange(
            new UserRetrospective { UserId = manager.Id, RetrospectiveId = retro.Id },
            new UserRetrospective { UserId = participant.Id, RetrospectiveId = retro.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var membership = new RecordingHubMembership();
        var controller = new UserAssignmentsController(
            context,
            userManager,
            new RetroAuthorizationService(context),
            membership)
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id, manager.Id),
        };

        var result = await controller.AssignUsers(new BatchAssignUsersRequest(retro.Id, [manager.Id]));

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal([(participant.Id, retro.Id)], membership.Removals);
    }

    [Fact]
    public async Task Reveal_NotifiesOpenRetrospectiveClients()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var retro = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        context.AddRange(organization, retro);
        await context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = new RetrospectivesController(
            new RetrospectiveService(new EfRetrospectiveRepository(context)),
            new RetroAuthorizationService(context),
            context,
            notifier,
            new NoOpClosedRetrospectiveActionItemMailer())
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id),
        };

        var result = await controller.Reveal(retro.Id);

        Assert.Equal(retro.Id, Assert.IsType<OkObjectResult>(result).Value);
        Assert.True((await context.Retrospectives.FindAsync(retro.Id))!.IsRevealed);
        Assert.Equal([retro.Id], notifier.RevealedNotifications);
    }

    [Fact]
    public async Task Reveal_WhenMissing_DoesNotNotify()
    {
        await using var context = CreateContext();
        var notifier = new RecordingLiveNotifier();
        var controller = new RetrospectivesController(
            new RetrospectiveService(new EfRetrospectiveRepository(context)),
            new RetroAuthorizationService(context),
            context,
            notifier,
            new NoOpClosedRetrospectiveActionItemMailer())
        {
            ControllerContext = ControllerContext(Roles.Manager),
        };

        var result = await controller.Reveal(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(notifier.RevealedNotifications);
    }

    [Fact]
    public async Task Close_NotifiesOpenRetrospectiveClients()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var retro = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        retro.Reveal();
        context.AddRange(organization, retro);
        await context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = new RetrospectivesController(
            new RetrospectiveService(new EfRetrospectiveRepository(context)),
            new RetroAuthorizationService(context),
            context,
            notifier,
            new NoOpClosedRetrospectiveActionItemMailer())
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id),
        };

        var result = await controller.Close(retro.Id, new RetroBackend.Dtos.CloseRetrospectiveRequest());

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotEqual(retro.Id, created.Value);
        Assert.Equal([retro.Id], notifier.ClosedNotifications);
    }

    [Fact]
    public async Task Delete_NotifiesOpenRetrospectiveClients()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var retro = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        context.AddRange(organization, retro);
        await context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = new RetrospectivesController(
            new RetrospectiveService(new EfRetrospectiveRepository(context)),
            new RetroAuthorizationService(context),
            context,
            notifier,
            new NoOpClosedRetrospectiveActionItemMailer())
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id),
        };

        var result = await controller.Delete(retro.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal([retro.Id], notifier.DeletedNotifications);
    }

    [Fact]
    public async Task GetById_ClosedWithoutOpenIteration_AllowsStartingNextIteration()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var retro = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        retro.Reveal();
        retro.Close();
        context.AddRange(organization, retro);
        await context.SaveChangesAsync();

        var dto = Assert.IsType<GetRetrospectiveDto>(
            Assert.IsType<OkObjectResult>(await CreateController(context, organization.Id).GetById(retro.Id)).Value);

        Assert.True(dto.IsClosed);
        Assert.True(dto.CanStartNextIteration);
    }

    [Fact]
    public async Task GetById_ClosedWithOpenIteration_DoesNotAllowStartingNextIteration()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var closed = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        closed.Reveal();
        closed.Close();
        var open = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        context.AddRange(organization, closed, open);
        await context.SaveChangesAsync();

        var dto = Assert.IsType<GetRetrospectiveDto>(
            Assert.IsType<OkObjectResult>(await CreateController(context, organization.Id).GetById(closed.Id)).Value);

        Assert.False(dto.CanStartNextIteration);
    }

    [Fact]
    public async Task GetById_OpenRetrospective_DoesNotAllowStartingNextIteration()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var retro = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        context.AddRange(organization, retro);
        await context.SaveChangesAsync();

        var dto = Assert.IsType<GetRetrospectiveDto>(
            Assert.IsType<OkObjectResult>(await CreateController(context, organization.Id).GetById(retro.Id)).Value);

        Assert.False(dto.CanStartNextIteration);
    }

    private static RetrospectivesController CreateController(RetroDbContext context, Guid organizationId) =>
        new(
            new RetrospectiveService(new EfRetrospectiveRepository(context)),
            new RetroAuthorizationService(context),
            context,
            new RecordingLiveNotifier(),
            new NoOpClosedRetrospectiveActionItemMailer())
        {
            ControllerContext = ControllerContext(Roles.Manager, organizationId),
        };

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"manager-rules-{Guid.NewGuid()}")
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

    private static UserAssignmentsController AssignmentsController(
        RetroDbContext context,
        UserManager<AppUser> userManager,
        Guid organizationId,
        string userId) =>
        new(context, userManager, new RetroAuthorizationService(context), new NoOpHubMembership())
        {
            ControllerContext = ControllerContext(Roles.Manager, organizationId, userId),
        };

    private static async Task<(Organization Organization, AppUser Owner, AppUser Outsider, Retrospective Retro)>
        SeedTwoManagersAndRetro(RetroDbContext context)
    {
        var organization = new Organization { Name = "Acme" };
        var owner = new AppUser("owner@acme.test", "owner")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "OWNER@ACME.TEST",
        };
        var outsider = new AppUser("outsider@acme.test", "outsider")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "OUTSIDER@ACME.TEST",
        };
        var managerRole = new IdentityRole(Roles.Manager)
        {
            NormalizedName = Roles.Manager.ToUpperInvariant(),
        };
        var retro = Retrospective.CreateNew(owner.Id, "Sprint", organization.Id);
        context.AddRange(organization, owner, outsider, managerRole, retro);
        context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = owner.Id, RoleId = managerRole.Id },
            new IdentityUserRole<string> { UserId = outsider.Id, RoleId = managerRole.Id });
        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = owner.Id,
            RetrospectiveId = retro.Id,
        });
        await context.SaveChangesAsync();
        return (organization, owner, outsider, retro);
    }

    private static ControllerContext ControllerContext(string role, Guid? organizationId = null, string userId = "actor")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
        };
        if (organizationId.HasValue)
            claims.Add(new Claim(AuthClaims.OrganizationId, organizationId.Value.ToString()));

        return new()
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
            },
        };
    }
}
