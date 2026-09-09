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

public class ListRetrospectiveBoardsTests
{
    [Fact]
    public async Task GetAll_GroupsSessionsByTitleAndReturnsOpenSession()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        var closed = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        closed.Close();
        var open = Retrospective.CreateNew("actor", "Sprint", organization.Id);
        var other = Retrospective.CreateNew("actor", "Planning", organization.Id);
        context.AddRange(organization, closed, open, other);
        await context.SaveChangesAsync();

        var page = Assert.IsType<PagedResponse<GetRetrospectiveBoardDto>>(
            Assert.IsType<OkObjectResult>(await CreateController(context, organization.Id).GetAll(organization.Id, 1, 20)).Value);

        Assert.Equal(2, page.TotalCount);
        var sprint = Assert.Single(page.Items, board => board.Title == "Sprint");
        Assert.Equal(2, sprint.SessionCount);
        Assert.Equal(open.Id, sprint.OpenSessionId);
        Assert.Equal(closed.Id, sprint.LatestClosedSessionId);
        Assert.Equal(other.Id, Assert.Single(page.Items, board => board.Title == "Planning").OpenSessionId);
    }

    [Fact]
    public async Task GetAll_PaginatesBoardsNotSessions()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.AddRange(
            organization,
            Retrospective.CreateNew("actor", "Alpha", organization.Id),
            Retrospective.CreateNew("actor", "Alpha", organization.Id),
            Retrospective.CreateNew("actor", "Beta", organization.Id));
        await context.SaveChangesAsync();

        var page = Assert.IsType<PagedResponse<GetRetrospectiveBoardDto>>(
            Assert.IsType<OkObjectResult>(await CreateController(context, organization.Id).GetAll(organization.Id, 1, 1)).Value);

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Alpha", page.Items[0].Title);
    }

    [Fact]
    public async Task GetSessions_ReturnsNewestFirstInPagesOfTwenty()
    {
        await using var context = CreateContext();
        var organization = new Organization { Name = "Acme" };
        context.Organizations.Add(organization);
        for (var i = 0; i < 21; i++)
            context.Retrospectives.Add(Retrospective.CreateNew("actor", "Sprint", organization.Id));
        context.Retrospectives.Add(Retrospective.CreateNew("actor", "Other", organization.Id));
        await context.SaveChangesAsync();

        var first = Assert.IsType<PagedResponse<GetRetrospectiveSummaryDto>>(
            Assert.IsType<OkObjectResult>(
                await CreateController(context, organization.Id).GetSessions("Sprint", organization.Id, 1, 20)).Value);
        var second = Assert.IsType<PagedResponse<GetRetrospectiveSummaryDto>>(
            Assert.IsType<OkObjectResult>(
                await CreateController(context, organization.Id).GetSessions("Sprint", organization.Id, 2, 20)).Value);

        Assert.Equal(21, first.TotalCount);
        Assert.Equal(20, first.Items.Count);
        Assert.Single(second.Items);
        Assert.All(first.Items, session => Assert.Equal("Sprint", session.Title));
        Assert.True(first.Items[0].CreatedAt >= first.Items[^1].CreatedAt);
    }

    [Fact]
    public async Task GetAll_ForbidsOtherOrganization()
    {
        await using var context = CreateContext();
        var own = new Organization { Name = "Acme" };
        var other = new Organization { Name = "Globex" };
        context.AddRange(own, other);
        await context.SaveChangesAsync();

        Assert.IsType<ForbidResult>(await CreateController(context, own.Id).GetAll(other.Id, 1, 20));
        Assert.IsType<ForbidResult>(await CreateController(context, own.Id).GetSessions("Sprint", other.Id));
    }

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
            .UseInMemoryDatabase($"list-boards-{Guid.NewGuid()}")
            .Options);
}
