using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetroBackend.Auth;
using RetroBackend.Models;

namespace RetroBackend.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<RetroDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        foreach (var role in new[] { Roles.Admin, Roles.Manager, Roles.StandardUser })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = configuration["DefaultAdmin:Email"] ?? "sa@remolon.com";
        var adminPassword = configuration["DefaultAdmin:Password"] ?? "Passw0rd!";
        var adminNickname = configuration["DefaultAdmin:Nickname"] ?? "sa";

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new AppUser(adminEmail, adminNickname);
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        var demoOrganizations = new[]
        {
            new { Name = "Acme", Slug = "acme" },
            new { Name = "Globex", Slug = "globex" },
            new { Name = "Initech", Slug = "initech" },
        };
        const string demoPassword = "Passw0rd!";

        foreach (var demo in demoOrganizations)
        {
            var organization = await db.Organizations
                .FirstOrDefaultAsync(o => o.Name.ToLower() == demo.Name.ToLower());
            if (organization is null)
            {
                organization = new Organization { Name = demo.Name };
                db.Organizations.Add(organization);
                await db.SaveChangesAsync();
            }

            var demoUsers = new[]
            {
                new { Prefix = "manager", Role = Roles.Manager },
                new { Prefix = "alice", Role = Roles.StandardUser },
                new { Prefix = "bob", Role = Roles.StandardUser },
            };
            foreach (var demoUser in demoUsers)
            {
                var email = $"{demoUser.Prefix}.{demo.Slug}@remolon.com";
                if (await userManager.FindByEmailAsync(email) is not null) continue;
                var user = new AppUser(email, $"{demoUser.Prefix}.{demo.Slug}")
                {
                    OrganizationId = organization.Id,
                };
                var result = await userManager.CreateAsync(user, demoPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, demoUser.Role);
            }
        }
    }
}
