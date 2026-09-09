using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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

public class ChangePasswordTests
{
    [Fact]
    public async Task ChangePassword_WithCurrentPassword_Succeeds()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice");
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, context, user.Id);

        var result = await controller.ChangePassword(new ChangePasswordRequest("Passw0rd!", "N3w-Passw0rd!"));

        Assert.IsType<NoContentResult>(result);
        Assert.True(await userManager.CheckPasswordAsync(user, "N3w-Passw0rd!"));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice");
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, context, user.Id);

        var result = await controller.ChangePassword(new ChangePasswordRequest("wrong-pass", "N3w-Passw0rd!"));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(await userManager.CheckPasswordAsync(user, "Passw0rd!"));
    }

    private static AuthController CreateController(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        string userId) =>
        new(
            userManager,
            context,
            new FakeEmailSender(),
            Options.Create(new EmailOptions { FrontendBaseUrl = "http://localhost:3000" }),
            NullLogger<AuthController>.Instance,
            new AuthTokenService(userManager, context, TestJwt.Configuration),
            new TestHostEnvironment())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId)],
                        "test")),
                },
            },
        };

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"change-password-{Guid.NewGuid()}")
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
}
