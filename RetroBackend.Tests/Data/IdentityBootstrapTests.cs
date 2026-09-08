using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RetroBackend.Auth;
using RetroBackend.Data;
using RetroBackend.Models;
using Xunit;

namespace RetroBackend.Tests.Data;

public class IdentityBootstrapTests
{
    [Fact]
    public async Task EnsureRolesAsync_CreatesManagerAndStandardUserRolesAndDoesNotCreateUsers()
    {
        await using var context = CreateContext();
        await using var services = BuildServices(context);

        await IdentityBootstrap.EnsureRolesAsync(services);
        await IdentityBootstrap.EnsureRolesAsync(services);

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True(await roleManager.RoleExistsAsync(Roles.Manager));
        Assert.True(await roleManager.RoleExistsAsync(Roles.StandardUser));
        Assert.Equal(2, await context.Roles.CountAsync());
        Assert.Equal(0, await context.Users.CountAsync());
    }

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"identity-bootstrap-{Guid.NewGuid()}")
            .Options);

    private static ServiceProvider BuildServices(RetroDbContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton<IRoleStore<IdentityRole>>(new RoleStore<IdentityRole>(context));
        services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.AddSingleton<IdentityErrorDescriber>();
        services.AddSingleton(Options.Create(new IdentityOptions()));
        services.AddLogging();
        services.AddSingleton<RoleManager<IdentityRole>>(sp =>
            new RoleManager<IdentityRole>(
                sp.GetRequiredService<IRoleStore<IdentityRole>>(),
                [],
                sp.GetRequiredService<ILookupNormalizer>(),
                sp.GetRequiredService<IdentityErrorDescriber>(),
                NullLogger<RoleManager<IdentityRole>>.Instance));
        return services.BuildServiceProvider();
    }
}
