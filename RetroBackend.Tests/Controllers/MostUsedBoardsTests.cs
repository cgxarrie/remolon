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

public class MostUsedBoardsTests
{
    [Fact]
    public async Task GetMostUsed_ReturnsEmptyWhenTheUserHasNotOpenedABoard()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Add(organization);
        context.Retrospectives.Add(Retrospective.CreateNew("actor", "Sprint", organization.Id));
        await context.SaveChangesAsync();

        var boards = await MostUsed(context, organization.Id);

        Assert.Empty(boards);
    }

    [Fact]
    public async Task RecordUse_CountsEverySessionOfTheSameBoard()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var older = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        older.Close();
        var current = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        context.Add(organization);
        context.AddRange(older, current);
        await context.SaveChangesAsync();

        var controller = CreateController(context, organization.Id);
        Assert.IsType<NoContentResult>(await controller.RecordUse(older.Id));
        Assert.IsType<NoContentResult>(await controller.RecordUse(current.Id));

        var usage = Assert.Single(context.BoardUsages);
        Assert.Equal("Sprint", usage.Title);
        Assert.Equal(2, usage.UseCount);
    }

    [Fact]
    public async Task GetMostUsed_ReturnsTheThreeBoardsOpenedMostOften()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var sprint = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        var planning = Retrospective.CreateNew("actor", "Planning", organization.Id);
        var retro = Retrospective.CreateNew("actor", "Retro", organization.Id);
        var standup = Retrospective.CreateNew("actor", "Standup", organization.Id);
        context.Add(organization);
        context.AddRange(sprint, planning, retro, standup);
        var now = DateTime.UtcNow;
        context.BoardUsages.AddRange(
            Usage("actor", organization.Id, "Sprint", 5, now.AddMinutes(-10)),
            Usage("actor", organization.Id, "Planning", 3, now.AddMinutes(-3)),
            Usage("actor", organization.Id, "Retro", 3, now.AddMinutes(-1)),
            Usage("actor", organization.Id, "Standup", 1, now));
        await context.SaveChangesAsync();

        var boards = await MostUsed(context, organization.Id);

        Assert.Equal(["Sprint", "Retro", "Planning"], boards.Select(board => board.Title));
        Assert.Equal(sprint.Id, boards[0].OpenSessionId);
    }

    [Fact]
    public async Task GetMostUsed_SkipsBoardsTheUserCanNoLongerOpen()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var hidden = Retrospective.CreateNew("someone-else", "Secret", organization.Id);
        var sprint = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        var planning = Retrospective.CreateNew("actor", "Planning", organization.Id);
        var retro = Retrospective.CreateNew("actor", "Retro", organization.Id);
        context.Add(organization);
        context.AddRange(hidden, sprint, planning, retro);
        var now = DateTime.UtcNow;
        context.BoardUsages.AddRange(
            Usage("actor", organization.Id, "Secret", 9, now),
            Usage("actor", organization.Id, "Sprint", 4, now.AddMinutes(-3)),
            Usage("actor", organization.Id, "Planning", 3, now.AddMinutes(-2)),
            Usage("actor", organization.Id, "Retro", 2, now.AddMinutes(-1)));
        await context.SaveChangesAsync();

        var boards = await MostUsed(context, organization.Id);

        Assert.Equal(["Sprint", "Planning", "Retro"], boards.Select(board => board.Title));
    }

    [Fact]
    public async Task GetMostUsed_IgnoresAnotherUsersOpens()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Add(organization);
        context.Retrospectives.Add(Retrospective.CreateNew("actor", "Sprint", organization.Id));
        context.BoardUsages.Add(Usage("other-user", organization.Id, "Sprint", 8, DateTime.UtcNow));
        await context.SaveChangesAsync();

        Assert.Empty(await MostUsed(context, organization.Id));
    }

    [Fact]
    public async Task RecordUse_ForbidsABoardTheUserIsNotAssignedTo()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var retro = Retrospective.CreateNew("someone-else", "Sprint", organization.Id);
        context.AddRange(organization, retro);
        await context.SaveChangesAsync();

        Assert.IsType<ForbidResult>(await CreateController(context, organization.Id).RecordUse(retro.Id));
        Assert.Empty(context.BoardUsages);
    }

    [Fact]
    public async Task GetMostUsed_LinksAClosedBoardToItsLatestSession()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var closed = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        closed.Close();
        context.AddRange(organization, closed);
        context.BoardUsages.Add(Usage("actor", organization.Id, "Sprint", 1, DateTime.UtcNow));
        await context.SaveChangesAsync();

        var board = Assert.Single(await MostUsed(context, organization.Id));
        Assert.Null(board.OpenSessionId);
        Assert.Equal(closed.Id, board.LatestClosedSessionId);
    }

    private static Task<List<GetRetrospectiveBoardDto>> MostUsed(RetroDbContext context, Guid organizationId) =>
        OkBoards(CreateController(context, organizationId).GetMostUsed());

    private static async Task<List<GetRetrospectiveBoardDto>> OkBoards(Task<IActionResult> action)
    {
        var result = Assert.IsType<OkObjectResult>(await action);
        return Assert.IsAssignableFrom<IReadOnlyList<GetRetrospectiveBoardDto>>(result.Value).ToList();
    }

    private static BoardUsage Usage(string userId, Guid organizationId, string title, int useCount, DateTime lastUsedAt) =>
        new()
        {
            UserId = userId,
            OrganizationId = organizationId,
            Title = title,
            UseCount = useCount,
            LastUsedAt = lastUsedAt,
        };

    private static RetrospectivesController CreateController(RetroDbContext context, Guid organizationId) =>
        new(
            new RetrospectiveService(new EfRetrospectiveRepository(context)),
            new RetroAuthorizationService(context),
            context,
            new RecordingLiveNotifier(),
            new NoOpClosedRetrospectiveActionItemMailer())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "actor"),
                        new Claim(ClaimTypes.Role, Roles.Manager),
                        new Claim(AuthClaims.OrganizationId, organizationId.ToString()),
                    ], "test")),
                },
            },
        };

    private static RetroDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"most-used-{Guid.NewGuid()}")
            .Options);
}
