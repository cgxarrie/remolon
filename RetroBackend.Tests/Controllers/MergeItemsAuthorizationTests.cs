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

public class MergeItemsAuthorizationTests
{
    [Fact]
    public async Task MergeItems_WhenTargetIsOnADifferentRetrospective_IsBadRequestAndDoesNotMerge()
    {
        var setup = await SeedTwoRetrosAsync();
        var controller = CreateController(setup.Context, setup.ManagerId, Roles.Manager);

        var result = await controller.MergeItems(
            setup.SourceItemId,
            new MergeItemRequest { TargetItemId = setup.OtherRetroItemId });

        Assert.IsType<BadRequestObjectResult>(result);
        var source = await setup.Context.Items.FindAsync(setup.SourceItemId);
        var target = await setup.Context.Items.FindAsync(setup.OtherRetroItemId);
        Assert.Null(source!.GroupId);
        Assert.Null(target!.GroupId);
    }

    [Fact]
    public async Task MergeItems_WhenTargetDoesNotExist_IsNotFound()
    {
        var setup = await SeedTwoRetrosAsync();
        var controller = CreateController(setup.Context, setup.ManagerId, Roles.Manager);

        var result = await controller.MergeItems(
            setup.SourceItemId,
            new MergeItemRequest { TargetItemId = Guid.NewGuid() });

        Assert.IsType<NotFoundResult>(result);
        Assert.Null((await setup.Context.Items.FindAsync(setup.SourceItemId))!.GroupId);
    }

    [Fact]
    public async Task MergeItems_WhenRetrospectiveIsClosed_IsConflict()
    {
        var setup = await SeedSingleRetroAsync(closed: true);
        var controller = CreateController(setup.Context, setup.ManagerId, Roles.Manager);

        var result = await controller.MergeItems(
            setup.SourceItemId,
            new MergeItemRequest { TargetItemId = setup.SameRetroTargetItemId });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Null((await setup.Context.Items.FindAsync(setup.SourceItemId))!.GroupId);
        Assert.Null((await setup.Context.Items.FindAsync(setup.SameRetroTargetItemId))!.GroupId);
    }

    [Fact]
    public async Task MergeItems_WhenItemsShareRevealedRetrospective_Succeeds()
    {
        var setup = await SeedSingleRetroAsync(closed: false);
        var controller = CreateController(setup.Context, setup.ManagerId, Roles.Manager);

        var result = await controller.MergeItems(
            setup.SourceItemId,
            new MergeItemRequest { TargetItemId = setup.SameRetroTargetItemId });

        var body = Assert.IsAssignableFrom<IEnumerable<GetItemDto>>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(2, body.Count());
        var groupId = Assert.Single(body.Select(i => i.GroupId).Distinct());
        Assert.NotNull(groupId);
    }

    private static ItemsController CreateController(RetroDbContext context, string userId, string role)
    {
        var authz = new RetroAuthorizationService(context);
        return new ItemsController(new ItemService(new EfItemRepository(context), authz), authz, new RecordingLiveNotifier())
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
    }

    private static async Task<SeededMerge> SeedTwoRetrosAsync()
    {
        var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var managerId = Guid.NewGuid().ToString();
        var sourceRetro = Retrospective.CreateNew(managerId, "Sprint A", organization.Id);
        var otherRetro = Retrospective.CreateNew(managerId, "Sprint B", organization.Id);
        sourceRetro.AddColumn(managerId, "Went Well", 1);
        otherRetro.AddColumn(managerId, "Went Well", 1);
        sourceRetro.Reveal();
        otherRetro.Reveal();

        context.Organizations.Add(organization);
        context.Retrospectives.AddRange(sourceRetro, otherRetro);
        await context.SaveChangesAsync();

        var sourceColumn = sourceRetro.Columns.Single(c => c.Title == "Went Well");
        var otherColumn = otherRetro.Columns.Single(c => c.Title == "Went Well");
        var sourceItem = new Item(managerId, "Manager", sourceColumn.Id, "Source", 0);
        var sameRetroTarget = new Item(managerId, "Manager", sourceColumn.Id, "Same retro", 1);
        var otherRetroItem = new Item(managerId, "Manager", otherColumn.Id, "Other retro", 0);
        context.Items.AddRange(sourceItem, sameRetroTarget, otherRetroItem);
        context.UserRetrospectives.AddRange(
            new UserRetrospective { UserId = managerId, RetrospectiveId = sourceRetro.Id },
            new UserRetrospective { UserId = managerId, RetrospectiveId = otherRetro.Id });
        await context.SaveChangesAsync();

        return new SeededMerge(
            context,
            managerId,
            sourceItem.Id,
            sameRetroTarget.Id,
            otherRetroItem.Id);
    }

    private static async Task<SeededMerge> SeedSingleRetroAsync(bool closed)
    {
        var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var managerId = Guid.NewGuid().ToString();
        var retro = Retrospective.CreateNew(managerId, "Sprint", organization.Id);
        retro.AddColumn(managerId, "Went Well", 1);
        retro.Reveal();
        if (closed) retro.Close();

        context.Organizations.Add(organization);
        context.Retrospectives.Add(retro);
        await context.SaveChangesAsync();

        var column = retro.Columns.Single(c => c.Title == "Went Well");
        var sourceItem = new Item(managerId, "Manager", column.Id, "Source", 0);
        var targetItem = new Item(managerId, "Manager", column.Id, "Target", 1);
        context.Items.AddRange(sourceItem, targetItem);
        context.UserRetrospectives.Add(new UserRetrospective
        {
            UserId = managerId,
            RetrospectiveId = retro.Id,
        });
        await context.SaveChangesAsync();

        return new SeededMerge(context, managerId, sourceItem.Id, targetItem.Id, Guid.Empty);
    }

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"merge-authz-{Guid.NewGuid()}")
            .Options);

    private sealed record SeededMerge(
        RetroDbContext Context,
        string ManagerId,
        Guid SourceItemId,
        Guid SameRetroTargetItemId,
        Guid OtherRetroItemId);
}
