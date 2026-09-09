using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetroBackend.Auth;
using RetroBackend.Controllers;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Models;
using RetroBackend.Repositories;
using RetroBackend.Services;
using RetroBackend.Tests.Fakes;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class ItemRevealRulesTests
{
    [Fact]
    public async Task UpdateItem_WhenUnrevealed_ManagerCannotEditOthersItem()
    {
        var setup = await SeedAsync(revealed: false);
        var controller = CreateController(setup.Context, setup.ManagerId, Roles.Manager);

        var result = await controller.UpdateItem(setup.OtherItemId, new Dtos.UpdateItemRequest { Description = "changed" });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateItem_WhenUnrevealed_CreatorCanEditOwnItem()
    {
        var setup = await SeedAsync(revealed: false);
        var controller = CreateController(setup.Context, setup.CreatorId, Roles.StandardUser);

        var result = await controller.UpdateItem(setup.OtherItemId, new Dtos.UpdateItemRequest { Description = "changed" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<GetItemDto>(ok.Value);
        Assert.Equal("changed", dto.Description);
    }

    [Fact]
    public async Task UpdateItem_WhenRevealed_ManagerCanEditOthersItem()
    {
        var setup = await SeedAsync(revealed: true);
        var controller = CreateController(setup.Context, setup.ManagerId, Roles.Manager);

        var result = await controller.UpdateItem(setup.OtherItemId, new Dtos.UpdateItemRequest { Description = "changed" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<GetItemDto>(ok.Value);
        Assert.Equal("changed", dto.Description);
    }

    [Fact]
    public async Task MergeItems_WhenUnrevealed_IsForbidden()
    {
        var setup = await SeedAsync(revealed: false);
        var controller = CreateController(setup.Context, setup.ManagerId, Roles.Manager);

        var result = await controller.MergeItems(setup.ManagerItemId, new MergeItemRequest { TargetItemId = setup.OtherItemId });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task MergeItems_WhenRevealed_ManagerCanMerge()
    {
        var setup = await SeedAsync(revealed: true);
        var controller = CreateController(setup.Context, setup.ManagerId, Roles.Manager);

        var result = await controller.MergeItems(setup.ManagerItemId, new MergeItemRequest { TargetItemId = setup.OtherItemId });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task MergeItems_WhenRevealed_StandardUserCannotMerge()
    {
        var setup = await SeedAsync(revealed: true);
        var controller = CreateController(setup.Context, setup.CreatorId, Roles.StandardUser);

        var result = await controller.MergeItems(setup.OtherItemId, new MergeItemRequest { TargetItemId = setup.ManagerItemId });

        Assert.IsType<ForbidResult>(result);
    }

    private static ItemsController CreateController(RetroDbContext context, string userId, string role) =>
        new(new ItemService(new EfItemRepository(context), new RetroAuthorizationService(context)), new RetroAuthorizationService(context), new RecordingLiveNotifier())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, userId),
                            new Claim(ClaimTypes.Role, role),
                        ],
                        "test")),
                },
            },
        };

    private static async Task<SeededRetro> SeedAsync(bool revealed)
    {
        var context = new RetroDbContext(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"item-reveal-{Guid.NewGuid()}")
            .Options);

        var organization = new Organization { Name = "Acme" };
        var managerId = Guid.NewGuid().ToString();
        var creatorId = Guid.NewGuid().ToString();
        var retro = Retrospective.CreateNew(managerId, "Sprint", organization.Id);
        retro.AddColumn(managerId, "Went Well", 1);
        if (revealed) retro.Reveal();

        context.Organizations.Add(organization);
        context.Retrospectives.Add(retro);
        await context.SaveChangesAsync();

        var column = retro.Columns.Single(c => c.Title == "Went Well");
        var managerItem = new Item(managerId, "Manager", column.Id, "Manager note", 0);
        var otherItem = new Item(creatorId, "Alice", column.Id, "Alice note", 1);
        context.Items.AddRange(managerItem, otherItem);
        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = managerId,
            RetrospectiveId = retro.Id,
        });
        await context.SaveChangesAsync();

        return new SeededRetro(context, managerId, creatorId, managerItem.Id, otherItem.Id);
    }

    private sealed record SeededRetro(
        RetroDbContext Context,
        string ManagerId,
        string CreatorId,
        Guid ManagerItemId,
        Guid OtherItemId);
}
