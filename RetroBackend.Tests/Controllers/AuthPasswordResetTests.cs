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
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class AuthPasswordResetTests
{
    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsGenericMessageAndDoesNotSendEmail()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var emails = new RecordingEmailSender();
        var controller = CreateController(userManager, context, emails);

        var result = await controller.ForgotPassword(new ForgotPasswordRequest("missing@example.com"));

        var response = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains("If an account exists", response.Message);
        Assert.Contains("30 minutes", response.Message);
        Assert.Empty(emails.Sent);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_SendsResetLinkAndDoesNotReturnToken()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice");
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var emails = new RecordingEmailSender();
        var controller = CreateController(userManager, context, emails);

        var result = await controller.ForgotPassword(new ForgotPasswordRequest("alice@example.com"));

        var response = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains("If an account exists", response.Message);
        var sent = Assert.Single(emails.Sent);
        Assert.Equal("alice@example.com", sent.To);
        Assert.Equal(PasswordResetEmail.Subject, sent.Subject);
        Assert.Contains("/reset-password#", sent.HtmlBody);
        Assert.Contains("alice%40example.com", sent.HtmlBody);
        Assert.DoesNotContain("resetToken", System.Text.Json.JsonSerializer.Serialize(response));
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_EmailFailureStillReturnsGenericMessage()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        Assert.True((await userManager.CreateAsync(new AppUser("alice@example.com", "alice"), "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, context, new ThrowingEmailSender());

        var result = await controller.ForgotPassword(new ForgotPasswordRequest("alice@example.com"));

        var response = Assert.IsType<ForgotPasswordResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Contains("If an account exists", response.Message);
    }

    private static AuthController CreateController(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        IEmailSender emailSender) =>
        new(
            userManager,
            context,
            emailSender,
            Options.Create(new EmailOptions { FrontendBaseUrl = "http://localhost:3000" }),
            NullLogger<AuthController>.Instance,
            new UnusedAuthTokenService());

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"auth-reset-{Guid.NewGuid()}")
            .Options);

    private static UserManager<AppUser> CreateUserManager(RetroDbContext context)
    {
        var userManager = new UserManager<AppUser>(
            new UserStore<AppUser>(context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<AppUser>>.Instance);
        userManager.RegisterTokenProvider(TokenOptions.DefaultProvider, new StaticResetTokenProvider());
        return userManager;
    }

    private sealed class StaticResetTokenProvider : IUserTwoFactorTokenProvider<AppUser>
    {
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<AppUser> manager, AppUser user) =>
            Task.FromResult(true);

        public Task<string> GenerateAsync(string purpose, UserManager<AppUser> manager, AppUser user) =>
            Task.FromResult("raw-reset-token");

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<AppUser> manager, AppUser user) =>
            Task.FromResult(token == "raw-reset-token");
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string HtmlBody)> Sent { get; } = [];

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("SMTP unavailable");
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
