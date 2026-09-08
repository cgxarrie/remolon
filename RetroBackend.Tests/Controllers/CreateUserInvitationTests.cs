using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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

public class CreateUserInvitationTests
{
    [Fact]
    public async Task CreateUser_SendsSetPasswordLinkAndOmitsPasswordFromResponse()
    {
        await using var context = CreateContext();
        var organization = await SeedOrganizationWithStandardRoleAsync(context);
        using var userManager = CreateUserManager(context);
        var emails = new RecordingEmailSender();
        var controller = CreateUsersController(userManager, context, organization.Id, emails);

        var result = await controller.Create(new CreateUserRequest(
            "new@acme.test", "newbie", Roles.StandardUser, organization.Id));

        var created = Assert.IsType<CreateUserResponse>(
            Assert.IsType<ObjectResult>(result).Value);
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.True(created.InvitationEmailSent);
        var json = JsonSerializer.Serialize(created);
        Assert.DoesNotContain("temporaryPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Temporary password", emails.Sent.Single().HtmlBody);
        Assert.Contains("/reset-password#", emails.Sent.Single().HtmlBody);
        Assert.Contains("purpose=invite", emails.Sent.Single().HtmlBody);
        Assert.Contains("valid for 30 days", emails.Sent.Single().HtmlBody);
        Assert.Contains("new%40acme.test", emails.Sent.Single().HtmlBody);

        var user = await userManager.FindByEmailAsync("new@acme.test");
        Assert.NotNull(user);
        Assert.Null(user!.PasswordHash);
        var claims = await userManager.GetClaimsAsync(user);
        Assert.Contains(claims, c => c.Type == AuthClaims.MustChangePassword && c.Value == "true");
    }

    [Fact]
    public async Task CreateUser_WhenEmailFails_StillOmitsPasswordFromResponse()
    {
        await using var context = CreateContext();
        var organization = await SeedOrganizationWithStandardRoleAsync(context);
        using var userManager = CreateUserManager(context);
        var controller = CreateUsersController(userManager, context, organization.Id, new ThrowingEmailSender());

        var result = await controller.Create(new CreateUserRequest(
            "new@acme.test", "newbie", Roles.StandardUser, organization.Id));

        var created = Assert.IsType<CreateUserResponse>(
            Assert.IsType<ObjectResult>(result).Value);
        Assert.False(created.InvitationEmailSent);
        var json = JsonSerializer.Serialize(created);
        Assert.DoesNotContain("temporaryPassword", json, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(await userManager.FindByEmailAsync("new@acme.test"));
    }

    [Fact]
    public async Task InviteLink_SetsPasswordAndClearsMustChange()
    {
        await using var context = CreateContext();
        var organization = await SeedOrganizationWithStandardRoleAsync(context);
        using var userManager = CreateUserManager(context);
        var emails = new RecordingEmailSender();
        var usersController = CreateUsersController(userManager, context, organization.Id, emails);
        await usersController.Create(new CreateUserRequest(
            "new@acme.test", "newbie", Roles.StandardUser, organization.Id));

        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("raw-reset-token"));
        var authController = new AuthController(
            userManager,
            new ConfigurationBuilder().Build(),
            context,
            emails,
            Options.Create(new EmailOptions { FrontendBaseUrl = "http://localhost:3000" }),
            NullLogger<AuthController>.Instance);

        var result = await authController.ResetPassword(new ResetPasswordRequest(
            "new@acme.test",
            encodedToken,
            "Correct-Horse-1!"));

        Assert.IsType<OkObjectResult>(result);
        var user = await userManager.FindByEmailAsync("new@acme.test");
        Assert.True(await userManager.CheckPasswordAsync(user!, "Correct-Horse-1!"));
        Assert.DoesNotContain((await userManager.GetClaimsAsync(user!)), c => c.Type == AuthClaims.MustChangePassword);
    }

    private static UsersController CreateUsersController(
        UserManager<AppUser> userManager,
        RetroDbContext context,
        Guid organizationId,
        IEmailSender emailSender) =>
        new(
            userManager,
            context,
            emailSender,
            Options.Create(new EmailOptions { FrontendBaseUrl = "http://localhost:3000" }),
            NullLogger<UsersController>.Instance,
            new UnusedAuthTokenService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, "manager"),
                            new Claim(ClaimTypes.Role, Roles.Manager),
                            new Claim(AuthClaims.OrganizationId, organizationId.ToString()),
                        ],
                        "test")),
                },
            },
        };

    private static async Task<Organization> SeedOrganizationWithStandardRoleAsync(RetroDbContext context)
    {
        var organization = new Organization { Name = "Acme" };
        var standardRole = new IdentityRole(Roles.StandardUser)
        {
            NormalizedName = Roles.StandardUser.ToUpperInvariant(),
        };
        context.AddRange(organization, standardRole);
        await context.SaveChangesAsync();
        return organization;
    }

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"invite-{Guid.NewGuid()}")
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
        userManager.RegisterTokenProvider(InvitationTokenProviderOptions.ProviderName, new StaticResetTokenProvider());
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
    }
}
