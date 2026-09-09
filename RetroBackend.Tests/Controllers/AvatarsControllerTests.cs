using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RetroBackend.Auth;
using RetroBackend.Controllers;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Models;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class AvatarsControllerTests
{
    private static readonly byte[] JpegBytes =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xD9
    ];

    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52
    ];

    [Fact]
    public async Task Upload_StoresAvatarAndReturnsUrl()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@acme.test", "alice") { OrganizationId = organization.Id };
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, user.Id, organization.Id);

        var result = await controller.Upload(JpegFile());

        var response = Assert.IsType<AvatarUploadResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal($"/api/users/{user.Id}/avatar?v={AvatarImage.VersionFor(JpegBytes)}", response.AvatarUrl);
        var stored = await userManager.FindByIdAsync(user.Id);
        Assert.Equal("image/jpeg", stored!.AvatarContentType);
        Assert.Equal(JpegBytes, stored.AvatarBytes);
    }

    [Fact]
    public async Task Upload_ReplacingAvatar_ReturnsDifferentUrl()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@acme.test", "alice") { OrganizationId = organization.Id };
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, user.Id, organization.Id);

        var first = await controller.Upload(JpegFile());
        var second = await controller.Upload(PngFile());

        var firstUrl = Assert.IsType<AvatarUploadResponse>(Assert.IsType<OkObjectResult>(first).Value).AvatarUrl;
        var secondUrl = Assert.IsType<AvatarUploadResponse>(Assert.IsType<OkObjectResult>(second).Value).AvatarUrl;
        Assert.NotEqual(firstUrl, secondUrl);
        var stored = await userManager.FindByIdAsync(user.Id);
        Assert.Equal(PngBytes, stored!.AvatarBytes);
        Assert.Equal("image/png", stored.AvatarContentType);
    }

    [Fact]
    public async Task Upload_RejectsNonImage()
    {
        await using var context = CreateContext();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@acme.test", "alice");
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, user.Id, null);

        var result = await controller.Upload(TextFile());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_ForbidsOtherOrganization()
    {
        await using var context = CreateContext();
        var acme = new Organization { Name = "Acme" };
        var globex = new Organization { Name = "Globex" };
        context.Organizations.AddRange(acme, globex);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var alice = new AppUser("alice@acme.test", "alice")
        {
            OrganizationId = acme.Id,
            AvatarBytes = JpegBytes,
            AvatarContentType = "image/jpeg",
        };
        var bob = new AppUser("bob@globex.test", "bob") { OrganizationId = globex.Id };
        Assert.True((await userManager.CreateAsync(alice, "Passw0rd!")).Succeeded);
        Assert.True((await userManager.CreateAsync(bob, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, bob.Id, globex.Id);

        var result = await controller.Get(alice.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Get_SameOrganization_ReturnsFile()
    {
        await using var context = CreateContext();
        var acme = new Organization { Name = "Acme" };
        context.Organizations.Add(acme);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var alice = new AppUser("alice@acme.test", "alice")
        {
            OrganizationId = acme.Id,
            AvatarBytes = JpegBytes,
            AvatarContentType = "image/jpeg",
        };
        var bob = new AppUser("bob@acme.test", "bob") { OrganizationId = acme.Id };
        Assert.True((await userManager.CreateAsync(alice, "Passw0rd!")).Succeeded);
        Assert.True((await userManager.CreateAsync(bob, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, bob.Id, acme.Id);

        controller.Request.QueryString = new QueryString($"?v={AvatarImage.VersionFor(JpegBytes)}");

        var result = await controller.Get(alice.Id);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Equal(JpegBytes, file.FileContents);
        Assert.Equal("private, max-age=3600", controller.Response.Headers.CacheControl.ToString());
        Assert.Equal("Authorization", controller.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task Get_WithoutCurrentVersion_IsNotCached()
    {
        await using var context = CreateContext();
        var acme = new Organization { Name = "Acme" };
        context.Organizations.Add(acme);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var alice = new AppUser("alice@acme.test", "alice")
        {
            OrganizationId = acme.Id,
            AvatarBytes = JpegBytes,
            AvatarContentType = "image/jpeg",
        };
        Assert.True((await userManager.CreateAsync(alice, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, alice.Id, acme.Id);
        controller.Request.QueryString = new QueryString($"?v={AvatarImage.VersionFor(PngBytes)}");

        var result = await controller.Get(alice.Id);

        Assert.IsType<FileContentResult>(result);
        Assert.Equal("private, no-cache", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Delete_ClearsStoredAvatar()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        using var userManager = CreateUserManager(context);
        var user = new AppUser("alice@acme.test", "alice")
        {
            OrganizationId = organization.Id,
            AvatarBytes = JpegBytes,
            AvatarContentType = "image/jpeg",
        };
        Assert.True((await userManager.CreateAsync(user, "Passw0rd!")).Succeeded);
        var controller = CreateController(userManager, user.Id, organization.Id);

        var result = await controller.Delete();

        Assert.IsType<NoContentResult>(result);
        var stored = await userManager.FindByIdAsync(user.Id);
        Assert.Null(stored!.AvatarBytes);
        Assert.Null(stored.AvatarContentType);
    }

    private static AvatarsController CreateController(UserManager<AppUser> userManager, string userId, Guid? organizationId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        if (organizationId.HasValue)
            claims.Add(new Claim(AuthClaims.OrganizationId, organizationId.Value.ToString()));
        return new AvatarsController(userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")),
                },
            },
        };
    }

    private static IFormFile JpegFile() => FormFile(JpegBytes, "photo.jpg", "image/jpeg");

    private static IFormFile PngFile() => FormFile(PngBytes, "photo.png", "image/png");

    private static IFormFile TextFile() => FormFile("not-an-image"u8.ToArray(), "notes.txt", "text/plain");

    private static IFormFile FormFile(byte[] bytes, string name, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"avatar-tests-{Guid.NewGuid()}")
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
}
