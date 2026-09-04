using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RetroBackend.Auth;
using RetroBackend.Config;
using RetroBackend.Controllers;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Models;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class MultiTenancyControllerTests
{
    [Fact]
    public async Task UsersGetAll_ManagerIsScopedAndReturnsOrderedPage()
    {
        await using var context = CreateContext();
        var own = new Organization { Name = "Acme" };
        var other = new Organization { Name = "Globex" };
        context.Organizations.AddRange(own, other);
        context.Users.AddRange(
            new AppUser("zoe@acme.test", "zoe") { OrganizationId = own.Id },
            new AppUser("alice@acme.test", "Alice") { OrganizationId = own.Id },
            new AppUser("other@globex.test", "other") { OrganizationId = other.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = CreateUsersController(userManager, context, Roles.Manager, own.Id);

        var result = await controller.GetAll(own.Id, 1, 1);
        var page = Assert.IsType<PagedResponse<UserSummaryDto>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal("Alice", Assert.Single(page.Items).Nickname);
        Assert.IsType<ForbidResult>(await controller.GetAll(other.Id, 1, 20));
    }

    [Fact]
    public async Task CreateUser_RejectsDuplicateNicknameCaseInsensitively()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        context.Users.Add(new AppUser("alice@acme.test", "Alice") { OrganizationId = organization.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = CreateUsersController(userManager, context, Roles.Admin, null);

        var result = await controller.Create(new CreateUserRequest(
            "alice2@acme.test", "alice", Roles.StandardUser, organization.Id));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateUser_ManagerCannotAssignAdminRole()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = CreateUsersController(userManager, context, Roles.Manager, organization.Id);

        var result = await controller.Create(new CreateUserRequest(
            "admin@acme.test", "admin.acme", Roles.Admin, organization.Id));

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task UpdateUserRole_ManagerCannotAssignAdminRole()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var controller = CreateUsersController(userManager, context, Roles.Manager, Guid.NewGuid());

        var result = await controller.UpdateRole(
            "target-user", new UpdateUserRoleRequest(Roles.Admin));

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteOrganization_RemovesUsersAndRetrospectives()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        context.Users.Add(new AppUser("manager@acme.test", "manager.acme") { OrganizationId = organization.Id });
        context.Retrospectives.Add(Retrospective.CreateNew("manager", "Sprint", organization.Id));
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = new OrganizationsController(context, userManager)
        {
            ControllerContext = ControllerContext(Roles.Admin, null),
        };

        Assert.IsType<NoContentResult>(await controller.Delete(organization.Id));
        Assert.Empty(context.Users);
        Assert.Empty(context.Retrospectives);
        Assert.Empty(context.Organizations);
    }

    [Fact]
    public async Task UpdateOrganization_ManagerCanUpdateOwnOrganization()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = new OrganizationsController(context, userManager)
        {
            ControllerContext = ControllerContext(Roles.Manager, organization.Id),
        };

        var result = await controller.Update(
            organization.Id,
            new SaveOrganizationRequest("Acme Updated", "ocean"));

        var updated = Assert.IsType<OrganizationDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("Acme Updated", updated.Name);
        Assert.Equal("ocean", updated.Theme.ThemeKey);
    }

    [Fact]
    public async Task UpdateOrganization_ManagerCannotUpdateDifferentOrganization()
    {
        await using var context = CreateContext();
        var own = new Organization { Name = "Acme" };
        var other = new Organization { Name = "Globex" };
        context.Organizations.AddRange(own, other);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = new OrganizationsController(context, userManager)
        {
            ControllerContext = ControllerContext(Roles.Manager, own.Id),
        };

        var result = await controller.Update(
            other.Id,
            new SaveOrganizationRequest("Changed", "forest"));

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Equal("Globex", (await context.Organizations.FindAsync(other.Id))!.Name);
        Assert.Equal("default", (await context.Organizations.FindAsync(other.Id))!.ThemeKey);
    }

    private static UsersController CreateUsersController(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        string role,
        Guid? organizationId) =>
        new(userManager, context, new FakeEmailSender(), EmailOptions(), NullLogger<UsersController>.Instance)
        {
            ControllerContext = ControllerContext(role, organizationId),
        };

    private static IOptions<EmailOptions> EmailOptions() =>
        Options.Create(new EmailOptions { FrontendBaseUrl = "http://localhost:3000" });

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"controller-tests-{Guid.NewGuid()}")
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

    private static ControllerContext ControllerContext(string role, Guid? organizationId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "actor"), new(ClaimTypes.Role, role) };
        if (organizationId.HasValue)
            claims.Add(new Claim(AuthClaims.OrganizationId, organizationId.Value.ToString()));
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
            },
        };
    }
}
