using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetroBackend.Auth;
using RetroBackend.Controllers;
using RetroBackend.Data;
using RetroBackend.Dtos;
using RetroBackend.Mappings;
using RetroBackend.Models;
using RetroBackend.Repositories;
using RetroBackend.Services;
using RetroBackend.Tests.Fakes;
using Xunit;

namespace RetroBackend.Tests.Controllers;

public class ItemCreationRulesTests
{
    [Fact]
    public async Task CreateActionItem_AssignedStandardUser_IsPersistedAndReturnedInRetrospective()
    {
        var setup = await SeedAsync();
        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedUserId, Roles.StandardUser, notifier);

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
        Assert.Equal([setup.RetrospectiveId], notifier.ItemsChangedNotifications);
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
    public async Task UpdateActionItem_AssignedManagerWhoDoesNotOwnRetrospective_IsAllowed()
    {
        var setup = await SeedAsync();
        var action = new ActionItem(setup.AssignedUserId, "Alice", "Alice", setup.ActionColumnId, "Original", 0);
        setup.Context.Items.Add(action);
        await setup.Context.SaveChangesAsync();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedManagerId, Roles.Manager);

        var result = await controller.UpdateActionItem(action.Id, new Dtos.UpdateActionItemRequest
        {
            Description = "Edited by assigned manager",
        });

        Assert.Equal("Edited by assigned manager",
            Assert.IsType<GetActionItemDto>(Assert.IsType<OkObjectResult>(result).Value).Description);
    }

    [Fact]
    public async Task CreateItem_WhenRetrospectiveIsClosed_IsConflict()
    {
        var setup = await SeedAsync(closed: true);
        var controller = new ItemsController(
            new ItemService(new EfItemRepository(setup.Context), new RetroAuthorizationService(setup.Context)),
            new RetroAuthorizationService(setup.Context),
            new RecordingLiveNotifier())
        {
            ControllerContext = ControllerContext(setup.AssignedUserId, Roles.StandardUser),
        };

        var result = await controller.CreateItem(new Dtos.CreateItemRequest
        {
            ColumnId = setup.ColumnId,
            Description = "Too late",
            Position = 0,
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdateActionItem_WhenRetrospectiveIsClosed_IsConflict()
    {
        var setup = await SeedAsync(closed: true);
        var action = new ActionItem(setup.AssignedUserId, "Alice", "Alice", setup.ActionColumnId, "Original", 0);
        setup.Context.Items.Add(action);
        await setup.Context.SaveChangesAsync();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedUserId, Roles.StandardUser);

        var result = await controller.UpdateActionItem(action.Id, new Dtos.UpdateActionItemRequest
        {
            Description = "Nope",
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreateActionItem_UnrelatedUser_IsForbidden()
    {
        var setup = await SeedAsync();
        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, "outsider", Roles.Manager, notifier);

        var result = await controller.CreateActionItem(new Dtos.CreateActionItemRequest
        {
            ColumnId = setup.ActionColumnId,
            Description = "Nope",
            Position = 0,
            Assignee = "all",
        });

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task CreateItem_AssignedManagerWhoDoesNotOwnRetrospective_IsAllowed()
    {
        var setup = await SeedAsync();
        var notifier = new RecordingLiveNotifier();
        var controller = new ItemsController(
            new ItemService(new EfItemRepository(setup.Context), new RetroAuthorizationService(setup.Context)),
            new RetroAuthorizationService(setup.Context),
            notifier)
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
        Assert.Equal([setup.RetrospectiveId], notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task CloseActionItem_NotifiesOpenRetrospectiveClients()
    {
        var setup = await SeedAsync();
        var pending = await AddPendingActionItemAsync(setup);

        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedUserId, Roles.StandardUser, notifier);

        var result = await controller.CloseActionItem(pending.Id);

        var dto = Assert.IsType<GetActionItemDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.True(dto.IsCompleted);
        Assert.Equal([setup.RetrospectiveId], notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task CloseActionItem_OwnerManagerWhoIsNotAssigned_IsAllowed()
    {
        var setup = await SeedAsync();
        var pending = await AddPendingActionItemAsync(setup);
        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, setup.OwnerId, Roles.Manager, notifier);

        var result = await controller.CloseActionItem(pending.Id);

        var dto = Assert.IsType<GetActionItemDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.True(dto.IsCompleted);
        Assert.Equal([setup.RetrospectiveId], notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task CloseActionItem_WhenMissing_IsForbidden()
    {
        var setup = await SeedAsync();
        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedUserId, Roles.StandardUser, notifier);

        var result = await controller.CloseActionItem(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task CloseActionItem_UnrelatedUser_IsForbidden()
    {
        var setup = await SeedAsync();
        var pending = await AddPendingActionItemAsync(setup);
        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, "outsider", Roles.Manager, notifier);

        var result = await controller.CloseActionItem(pending.Id);

        Assert.IsType<ForbidResult>(result);
        Assert.False(pending.IsCompleted);
        Assert.Empty(notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task CloseActionItem_OtherOrganizationUser_IsForbidden()
    {
        var setup = await SeedAsync();
        var pending = await AddPendingActionItemAsync(setup);

        var globex = new Organization { Name = "Globex" };
        setup.Context.Organizations.Add(globex);
        await setup.Context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, "globex-user", Roles.StandardUser, notifier);

        var result = await controller.CloseActionItem(pending.Id);

        Assert.IsType<ForbidResult>(result);
        Assert.False(pending.IsCompleted);
        Assert.Empty(notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task UpdateItem_NotifiesOpenRetrospectiveClients()
    {
        var setup = await SeedAsync();
        var item = new Item(setup.AssignedUserId, "Alice", setup.ColumnId, "Original", 0);
        setup.Context.Items.Add(item);
        await setup.Context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = new ItemsController(
            new ItemService(new EfItemRepository(setup.Context), new RetroAuthorizationService(setup.Context)),
            new RetroAuthorizationService(setup.Context),
            notifier)
        {
            ControllerContext = ControllerContext(setup.AssignedUserId, Roles.StandardUser),
        };

        var result = await controller.UpdateItem(item.Id, new Dtos.UpdateItemRequest { Description = "Edited" });

        Assert.Equal("Edited", Assert.IsType<GetItemDto>(Assert.IsType<OkObjectResult>(result).Value).Description);
        Assert.Equal([setup.RetrospectiveId], notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task DeleteItem_NotifiesOpenRetrospectiveClients()
    {
        var setup = await SeedAsync();
        var item = new Item(setup.AssignedUserId, "Alice", setup.ColumnId, "To delete", 0);
        setup.Context.Items.Add(item);
        await setup.Context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = new ItemsController(
            new ItemService(new EfItemRepository(setup.Context), new RetroAuthorizationService(setup.Context)),
            new RetroAuthorizationService(setup.Context),
            notifier)
        {
            ControllerContext = ControllerContext(setup.AssignedUserId, Roles.StandardUser),
        };

        var result = await controller.DeleteItem(item.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal([setup.RetrospectiveId], notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task UpdateItem_WhenForbidden_DoesNotNotify()
    {
        var setup = await SeedAsync();
        var item = new Item(setup.AssignedUserId, "Alice", setup.ColumnId, "Original", 0);
        setup.Context.Items.Add(item);
        await setup.Context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = new ItemsController(
            new ItemService(new EfItemRepository(setup.Context), new RetroAuthorizationService(setup.Context)),
            new RetroAuthorizationService(setup.Context),
            notifier)
        {
            ControllerContext = ControllerContext("outsider", Roles.StandardUser),
        };

        var result = await controller.UpdateItem(item.Id, new Dtos.UpdateItemRequest { Description = "Nope" });

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task UpdateActionItem_NotifiesOpenRetrospectiveClients()
    {
        var setup = await SeedAsync();
        var action = new ActionItem(setup.AssignedUserId, "Alice", "Alice", setup.ActionColumnId, "Original", 0);
        setup.Context.Items.Add(action);
        await setup.Context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedUserId, Roles.StandardUser, notifier);

        var result = await controller.UpdateActionItem(action.Id, new Dtos.UpdateActionItemRequest
        {
            Description = "Edited action",
            Assignee = "Bob",
        });

        Assert.Equal("Edited action", Assert.IsType<GetActionItemDto>(Assert.IsType<OkObjectResult>(result).Value).Description);
        Assert.Equal([setup.RetrospectiveId], notifier.ItemsChangedNotifications);
    }

    [Fact]
    public async Task DeleteActionItem_NotifiesOpenRetrospectiveClients()
    {
        var setup = await SeedAsync();
        var action = new ActionItem(setup.AssignedUserId, "Alice", "Alice", setup.ActionColumnId, "To delete", 0);
        setup.Context.Items.Add(action);
        await setup.Context.SaveChangesAsync();

        var notifier = new RecordingLiveNotifier();
        var controller = CreateActionItemsController(setup.Context, setup.AssignedUserId, Roles.StandardUser, notifier);

        var result = await controller.DeleteActionItem(action.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal([setup.RetrospectiveId], notifier.ItemsChangedNotifications);
    }

    private static async Task<ActionItem> AddPendingActionItemAsync(Setup setup)
    {
        var pendingColumnId = setup.Context.Columns.Single(c => c.Title == "Pending Action Items").Id;
        var pending = new ActionItem(setup.AssignedUserId, "Alice", "Alice", pendingColumnId, "Carry over", 0);
        setup.Context.Items.Add(pending);
        await setup.Context.SaveChangesAsync();
        return pending;
    }

    private static ActionItemsController CreateActionItemsController(
        RetroDbContext context,
        string userId,
        string role,
        IRetrospectiveLiveNotifier? notifier = null) =>
        new(
            new ItemService(new EfItemRepository(context), new RetroAuthorizationService(context)),
            new RetroAuthorizationService(context),
            notifier ?? new RecordingLiveNotifier())
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
                    new Claim(ClaimTypes.Email, $"{userId}@test.local"),
                ],
                "test")),
        },
    };

    private static async Task<Setup> SeedAsync(bool closed = false)
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
        if (closed) retro.Close();

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
