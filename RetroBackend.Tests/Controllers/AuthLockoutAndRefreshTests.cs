using System.IdentityModel.Tokens.Jwt;
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
using RetroBackend.Tests.Config;
using RetroBackend.Tests.Fakes;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class AuthLockoutAndRefreshTests
{
    private const string Password = "Correct-Horse-1!";

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccount()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice") { LockoutEnabled = true };
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        var controller = CreateController(userManager, context);

        for (var i = 0; i < 5; i++)
        {
            var failed = await controller.Login(new LoginRequest("alice@example.com", "wrong-password"));
            Assert.IsType<UnauthorizedObjectResult>(failed);
        }

        Assert.True(await userManager.IsLockedOutAsync(user));

        var stillLocked = await controller.Login(new LoginRequest("alice@example.com", Password));
        Assert.IsType<UnauthorizedObjectResult>(stillLocked);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndRejectsReuse()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice");
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        var tokens = new AuthTokenService(userManager, context, TestJwt.Configuration);
        var issued = await tokens.BuildAuthResponseAsync(user, Roles.StandardUser);

        var first = await tokens.RefreshAsync(issued.RefreshToken!);
        Assert.NotNull(first);
        Assert.NotEqual(issued.RefreshToken, first!.RefreshToken);

        var second = await tokens.RefreshAsync(first.RefreshToken!);
        Assert.NotNull(second);
        Assert.NotEqual(first.RefreshToken, second!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ReuseOfRotatedToken_RevokesRemainingSessions()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice");
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        var tokens = new AuthTokenService(userManager, context, TestJwt.Configuration);
        var issued = await tokens.BuildAuthResponseAsync(user, Roles.StandardUser);
        var rotated = await tokens.RefreshAsync(issued.RefreshToken!);
        Assert.NotNull(rotated);

        Assert.Null(await tokens.RefreshAsync(issued.RefreshToken!));
        Assert.Null(await tokens.RefreshAsync(rotated!.RefreshToken!));
        Assert.All(context.RefreshTokens.ToList(), token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task AccessToken_IncludesSecurityStamp_ThatChangesAfterRoleStampUpdate()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice");
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        var tokens = new AuthTokenService(userManager, context, TestJwt.Configuration);
        var issued = await tokens.BuildAuthResponseAsync(user, Roles.StandardUser);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);
        var stampClaim = jwt.Claims.First(c => c.Type == AuthClaims.SecurityStamp).Value;
        Assert.Equal(await userManager.GetSecurityStampAsync(user), stampClaim);

        await userManager.UpdateSecurityStampAsync(user);
        Assert.NotEqual(stampClaim, await userManager.GetSecurityStampAsync(user));
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(16));
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(10));
    }

    [Fact]
    public void AuthTokenResponse_JsonOmitsSecrets()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new AuthTokenResponse(
                "secret-access",
                "alice@example.com",
                Roles.StandardUser,
                "alice",
                "Acme",
                "secret-refresh",
                "user-1",
                Guid.NewGuid().ToString()),
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        Assert.Contains("alice@example.com", json);
        Assert.DoesNotContain("secret-access", json);
        Assert.DoesNotContain("secret-refresh", json);
        Assert.DoesNotContain("\"token\"", json);
        Assert.DoesNotContain("refreshToken", json);
    }

    private static AuthController CreateController(UserManager<AppUser> userManager, RetroDbContext context) =>
        new(
            userManager,
            context,
            new FakeEmailSender(),
            Options.Create(new EmailOptions { FrontendBaseUrl = "http://localhost:3000" }),
            NullLogger<AuthController>.Instance,
            new AuthTokenService(userManager, context, TestJwt.Configuration),
            new TestHostEnvironment())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"auth-lockout-{Guid.NewGuid()}")
            .Options);

    private static UserManager<AppUser> CreateUserManager(RetroDbContext context)
    {
        var options = new IdentityOptions();
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        IdentityPasswordPolicy.Apply(options);
        return new UserManager<AppUser>(
            new UserStore<AppUser>(context),
            Options.Create(options),
            new PasswordHasher<AppUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<AppUser>>.Instance);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
