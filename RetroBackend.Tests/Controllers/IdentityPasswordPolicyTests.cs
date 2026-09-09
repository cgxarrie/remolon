using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RetroBackend.Auth;
using RetroBackend.Data;
using RetroBackend.Models;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class IdentityPasswordPolicyTests
{
    [Fact]
    public async Task CreateUser_RejectsPassword1()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice");

        var result = await userManager.CreateAsync(user, "Password1");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code is "PasswordTooShort" or "PasswordRequiresNonAlphanumeric");
    }

    [Fact]
    public async Task CreateUser_AcceptsPasswordMeetingComplexityRules()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@example.com", "alice");

        var result = await userManager.CreateAsync(user, "Correct-Horse-1!");

        Assert.True(result.Succeeded, string.Join(' ', result.Errors.Select(e => e.Description)));
    }

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"password-policy-{Guid.NewGuid()}")
            .Options);

    private static UserManager<AppUser> CreateUserManager(RetroDbContext context)
    {
        var identityOptions = new IdentityOptions();
        IdentityPasswordPolicy.Apply(options: identityOptions);
        return new UserManager<AppUser>(
            new UserStore<AppUser>(context),
            Options.Create(identityOptions),
            new PasswordHasher<AppUser>(),
            [new UserValidator<AppUser>()],
            [new PasswordValidator<AppUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<AppUser>>.Instance);
    }
}
