using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

public class UserProfileTests
{
    [Fact]
    public async Task GetMe_ReturnsCurrentUserProfile()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@acme.test", "alice") { OrganizationId = organization.Id };
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateUsersController(userManager, context, user.Id, Roles.StandardUser, organization.Id);

        var result = await controller.GetMe();

        var dto = Assert.IsType<CurrentUserDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("alice@acme.test", dto.Email);
        Assert.Equal("alice", dto.Nickname);
        Assert.Equal(Roles.StandardUser, dto.Role);
        Assert.Null(dto.AvatarUrl);
    }

    [Fact]
    public async Task UpdateMe_ChangesNicknameAndReturnsNewToken()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@acme.test", "alice") { OrganizationId = organization.Id };
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateUsersController(userManager, context, user.Id, Roles.StandardUser, organization.Id);

        var result = await controller.UpdateMe(new UpdateMeRequest("alice.updated"));

        var response = Assert.IsType<AuthTokenResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("alice.updated", response.Nickname);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("alice.updated", (await userManager.FindByIdAsync(user.Id))!.Nickname);
    }

    [Fact]
    public async Task UpdateMe_RejectsDuplicateNickname()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        context.Users.Add(new AppUser("bob@acme.test", "bob") { OrganizationId = organization.Id });
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@acme.test", "alice") { OrganizationId = organization.Id };
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateUsersController(userManager, context, user.Id, Roles.StandardUser, organization.Id);

        var result = await controller.UpdateMe(new UpdateMeRequest("BOB"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("alice", (await userManager.FindByIdAsync(user.Id))!.Nickname);
    }

    [Fact]
    public async Task UpdateMe_RejectsWhitespaceNickname()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@acme.test", "alice");
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateUsersController(userManager, context, user.Id, Roles.StandardUser, null);

        var result = await controller.UpdateMe(new UpdateMeRequest("   "));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static UsersController CreateUsersController(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        string userId,
        string role,
        Guid? organizationId) =>
        new(
            userManager,
            context,
            new FakeEmailSender(),
            Options.Create(new EmailOptions { FrontendBaseUrl = "http://localhost:3000" }),
            NullLogger<UsersController>.Instance,
            new AuthTokenService(userManager, context, TestJwtConfiguration()))
        {
            ControllerContext = ControllerContext(userId, role, organizationId),
        };

    private static IConfiguration TestJwtConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "unit-test-jwt-signing-key-32ch!!",
                ["Jwt:Issuer"] = "RetroBackend",
                ["Jwt:Audience"] = "RetroBackendClients",
            })
            .Build();

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"profile-tests-{Guid.NewGuid()}")
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

    private static ControllerContext ControllerContext(string userId, string role, Guid? organizationId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId), new(ClaimTypes.Role, role) };
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

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
