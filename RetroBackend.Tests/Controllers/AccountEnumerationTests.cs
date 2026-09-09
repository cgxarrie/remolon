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
using RetroBackend.Tests.Fakes;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class AccountEnumerationTests
{
    [Fact]
    public async Task AssignUser_UnknownEmailAndOtherOrgEmail_ReturnSameError()
    {
        await using var context = CreateContext();
        var (organization, owner, retro) = await SeedAssignedManagerRetro(context);
        var otherOrg = new Organization { Name = "Globex" };
        var foreign = new AppUser("alice@globex.test", "alice")
        {
            OrganizationId = otherOrg.Id,
            NormalizedEmail = "ALICE@GLOBEX.TEST",
        };
        context.AddRange(otherOrg, foreign);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var controller = AssignmentsController(context, userManager, organization.Id, owner.Id);

        var missing = await controller.AssignUser(new AssignUserRequest("missing@example.com", retro.Id));
        var otherOrgResult = await controller.AssignUser(new AssignUserRequest(foreign.Email!, retro.Id));

        var missingBody = Assert.IsType<BadRequestObjectResult>(missing);
        var otherBody = Assert.IsType<BadRequestObjectResult>(otherOrgResult);
        Assert.Equal(Serialize(missingBody.Value), Serialize(otherBody.Value));
    }

    [Fact]
    public async Task CreateUser_EmailInAnotherOrg_DoesNotSayExists()
    {
        await using var context = CreateContext();
        var acme = new Organization { Name = "Acme" };
        var globex = new Organization { Name = "Globex" };
        context.AddRange(acme, globex, new IdentityRole(Roles.StandardUser)
        {
            NormalizedName = Roles.StandardUser.ToUpperInvariant(),
        });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        Assert.True((await userManager.CreateAsync(
            new AppUser("taken@globex.test", "taken") { OrganizationId = globex.Id })).Succeeded);
        var controller = UsersController(userManager, context, acme.Id);

        var result = await controller.Create(new CreateUserRequest(
            "taken@globex.test", "newbie", Roles.StandardUser, acme.Id));

        var body = Assert.IsType<BadRequestObjectResult>(result);
        Assert.DoesNotContain("exists", Serialize(body.Value), StringComparison.OrdinalIgnoreCase);
    }

    private static string Serialize(object? value) =>
        System.Text.Json.JsonSerializer.Serialize(value);

    private static UserAssignmentsController AssignmentsController(
        RetroDbContext context,
        UserManager<AppUser> userManager,
        Guid organizationId,
        string userId) =>
        new(context, userManager, new RetroAuthorizationService(context), new NoOpHubMembership())
        {
            ControllerContext = ControllerContext(organizationId, userId),
        };

    private static UsersController UsersController(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        Guid organizationId) =>
        new(
            userManager,
            context,
            new FakeEmailSender(),
            Options.Create(new EmailOptions { FrontendBaseUrl = "http://localhost:3000" }),
            NullLogger<UsersController>.Instance,
            new UnusedAuthTokenService())
        {
            ControllerContext = ControllerContext(organizationId, "manager"),
        };

    private static ControllerContext ControllerContext(Guid organizationId, string userId) =>
        new()
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Role, Roles.Manager),
                        new Claim(AuthClaims.OrganizationId, organizationId.ToString()),
                    ],
                    "test")),
            },
        };

    private static async Task<(Organization Organization, AppUser Owner, Retrospective Retro)>
        SeedAssignedManagerRetro(RetroDbContext context)
    {
        var organization = new Organization { Name = "Acme" };
        var owner = new AppUser("owner@acme.test", "owner")
        {
            OrganizationId = organization.Id,
            NormalizedEmail = "OWNER@ACME.TEST",
        };
        var retro = Retrospective.CreateNew(owner.Id, "Sprint", organization.Id);
        context.AddRange(organization, owner, retro);
        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = owner.Id,
            RetrospectiveId = retro.Id,
        });
        await context.SaveChangesAsync();
        return (organization, owner, retro);
    }

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"enum-{Guid.NewGuid()}")
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

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class UnusedAuthTokenService : IAuthTokenService
    {
        public Task<AuthTokenResponse> BuildAuthResponseAsync(AppUser user, string role) =>
            throw new NotSupportedException();

        public Task<AuthTokenResponse?> RefreshAsync(string refreshToken) =>
            throw new NotSupportedException();

        public Task RevokeAllForUserAsync(string userId) => Task.CompletedTask;

        public Task RevokeAsync(string refreshToken) => Task.CompletedTask;
    }
}
