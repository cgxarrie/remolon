using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetroBackend.Auth;
using RetroBackend.Controllers;
using RetroBackend.Data;
using RetroBackend.Mappings;
using RetroBackend.Models;
using RetroBackend.Repositories;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class ItemCreationRulesTests
{
    [Fact]
    public async Task CreateActionItem_AssignedStandardUser_IsPersistedAndReturnedInRetrospective()
    {
        var setup = await SeedAsync();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedUserId, Roles.StandardUser);

        var result = await controller.CreateActionItem(new Dtos.CreateActionItemRequest
        {
            ColumnId = setup.ActionColumnId,
            Description = "Do the thing",
            Position = 0,
            Assignee = "Alice",
        });

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);

        var retro = await new RetrospectiveService(new EfRetrospectiveRepository(setup.Context))
            .GetByIdAsync(setup.RetrospectiveId);
        var column = retro!.ToGetDto(setup.AssignedUserId).ActionColumns
            .Single(c => c.Title == "Action Items");
        var item = Assert.Single(column.Items);
        Assert.Equal("Do the thing", item.Description);
        Assert.Equal("Alice", item.Assignee);
    }

    [Fact]
    public async Task CreateActionItem_AssignedManagerWhoDoesNotOwnRetrospective_IsAllowed()
    {
        var setup = await SeedAsync();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedManagerId, Roles.Manager);

        var result = await controller.CreateActionItem(new Dtos.CreateActionItemRequest
        {
            ColumnId = setup.ActionColumnId,
            Description = "Manager action",
            Position = 0,
            Assignee = "all",
        });

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task CreateActionItem_OwnerManagerWhoIsNotAssigned_IsAllowed()
    {
        var setup = await SeedAsync();
        var controller = CreateActionItemsController(setup.Context, setup.OwnerId, Roles.Manager);

        var result = await controller.CreateActionItem(new Dtos.CreateActionItemRequest
        {
            ColumnId = setup.ActionColumnId,
            Description = "Owner action",
            Position = 0,
            Assignee = "all",
        });

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task CreateActionItem_UnrelatedUser_IsForbidden()
    {
        var setup = await SeedAsync();
        var controller = CreateActionItemsController(setup.Context, "outsider", Roles.Manager);

        var result = await controller.CreateActionItem(new Dtos.CreateActionItemRequest
        {
            ColumnId = setup.ActionColumnId,
            Description = "Nope",
            Position = 0,
            Assignee = "all",
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateItem_AssignedManagerWhoDoesNotOwnRetrospective_IsAllowed()
    {
        var setup = await SeedAsync();
        var controller = new ItemsController(
            new ItemService(new EfItemRepository(setup.Context)),
            new RetroAuthorizationService(setup.Context))
        {
            ControllerContext = ControllerContext(setup.AssignedManagerId, Roles.Manager),
        };

        var result = await controller.CreateItem(new Dtos.CreateItemRequest
        {
            ColumnId = setup.ColumnId,
            Description = "Manager note",
            Position = 0,
        });

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    private static ActionItemsController CreateActionItemsController(
        RetroDbContext context,
        string userId,
        string role) =>
        new(new ItemService(new EfItemRepository(context)), new RetroAuthorizationService(context))
        {
            ControllerContext = ControllerContext(userId, role),
        };

    private static ControllerContext ControllerContext(string userId, string role) => new()
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
    };

    private static async Task<Setup> SeedAsync()
    {
        var context = new RetroDbContext(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"item-creation-{Guid.NewGuid()}")
            .Options);

        var organization = new Organization { Name = "Acme" };
        var ownerId = "manager-owner";
        var assignedManagerId = "manager-assigned";
        var assignedUserId = "user-assigned";

        var retro = Retrospective.CreateNew(ownerId, "Sprint", organization.Id);
        retro.AddColumn(ownerId, "Went Well", 1);
        retro.Reveal();

        context.Organizations.Add(organization);
        context.Retrospectives.Add(retro);
        await context.SaveChangesAsync();

        context.UserRetrospectives.AddRange(
            new UserRetrospective { UserId = assignedManagerId, RetrospectiveId = retro.Id },
            new UserRetrospective { UserId = assignedUserId, RetrospectiveId = retro.Id });
        await context.SaveChangesAsync();

        return new Setup(
            context,
            retro.Id,
            retro.Columns.Single(c => c.Title == "Action Items").Id,
            retro.Columns.Single(c => c.Title == "Went Well").Id,
            ownerId,
            assignedManagerId,
            assignedUserId);
    }

    private sealed record Setup(
        RetroDbContext Context,
        Guid RetrospectiveId,
        Guid ActionColumnId,
        Guid ColumnId,
        string OwnerId,
        string AssignedManagerId,
        string AssignedUserId);
}
