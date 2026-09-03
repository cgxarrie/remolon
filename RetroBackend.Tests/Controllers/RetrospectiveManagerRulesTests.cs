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
        var controller = new RetrospectivesController(service, new RetroAuthorizationService(context), context)
        {
            ControllerContext = ControllerContext(Roles.Admin),
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
        var controller = new UserAssignmentsController(context, userManager, new RetroAuthorizationService(context))
        {
            ControllerContext = ControllerContext(Roles.Admin),
        };

        var result = await controller.UnassignUser(new AssignUserRequest(manager.Email!, retro.Id));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(await context.UserRetrospectives.AnyAsync(
            assignment => assignment.UserId == manager.Id && assignment.RetrospectiveId == retro.Id));
    }

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

    private static ControllerContext ControllerContext(string role) =>
        new()
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "actor"),
                        new Claim(ClaimTypes.Role, role),
                    ],
                    "test")),
            },
        };
}
