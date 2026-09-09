using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RetroBackend.Data;
using RetroBackend.Models;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class ClosedRetrospectiveActionItemMailerTests
{
    [Fact]
    public async Task NotifyAsync_SendsOpenPendingAndActionItemsToNamedAssignees()
    {
        var setup = await SeedAsync();
        var emails = new RecordingEmailSender();
        var mailer = CreateMailer(setup.Context, emails);

        var pendingId = setup.Retro.Columns.OfType<ActionColumn>().Single(c => c.Title == "Pending Action Items").Id;
        var actionId = setup.Retro.Columns.OfType<ActionColumn>().Single(c => c.Title == "Action Items").Id;
        setup.Context.Items.Add(new ActionItem("owner", "Mgr", ["Alice", "Bob"], pendingId, "Finish docs", 0, 2));
        var completed = new ActionItem("owner", "Mgr", ["Alice"], pendingId, "Already done", 1);
        completed.Complete("owner");
        setup.Context.Items.Add(completed);
        setup.Context.Items.Add(new ActionItem("owner", "Mgr", ["Carol"], actionId, "Ship feature", 0));
        await setup.Context.SaveChangesAsync();

        await mailer.NotifyAsync(await ReloadAsync(setup));

        Assert.Equal(3, emails.Sent.Count);
        Assert.Contains(emails.Sent, email =>
            email.To == "alice@acme.test"
            && email.Subject.StartsWith("Pending action item")
            && email.HtmlBody.Contains("Finish docs")
            && email.HtmlBody.Contains("Sessions:</strong> 2")
            && email.HtmlBody.Contains("Alice, Bob"));
        Assert.Contains(emails.Sent, email => email.To == "bob@acme.test" && email.HtmlBody.Contains("Finish docs"));
        Assert.Contains(emails.Sent, email =>
            email.To == "carol@acme.test"
            && email.Subject.StartsWith("Action item")
            && email.HtmlBody.Contains("Ship feature")
            && email.HtmlBody.Contains("Sessions:</strong> 1"));
        Assert.DoesNotContain(emails.Sent, email => email.HtmlBody.Contains("Already done"));
    }

    [Fact]
    public async Task NotifyAsync_WhenAssignedToAll_SendsToEveryParticipant()
    {
        var setup = await SeedAsync();
        var emails = new RecordingEmailSender();
        var mailer = CreateMailer(setup.Context, emails);

        var actionId = setup.Retro.Columns.OfType<ActionColumn>().Single(c => c.Title == "Action Items").Id;
        setup.Context.Items.Add(new ActionItem("owner", "Mgr", ["all"], actionId, "Team cleanup", 0));
        await setup.Context.SaveChangesAsync();

        await mailer.NotifyAsync(await ReloadAsync(setup));

        Assert.Equal(3, emails.Sent.Count);
        Assert.Equal(
            ["alice@acme.test", "bob@acme.test", "carol@acme.test"],
            emails.Sent.Select(email => email.To).OrderBy(to => to).ToArray());
        Assert.All(emails.Sent, email => Assert.Contains("Team cleanup", email.HtmlBody));
        Assert.DoesNotContain(emails.Sent, email => email.To == "dave@acme.test");
    }

    [Fact]
    public async Task NotifyAsync_WhenUnassigned_DoesNotSend()
    {
        var setup = await SeedAsync();
        var emails = new RecordingEmailSender();
        var mailer = CreateMailer(setup.Context, emails);

        var actionId = setup.Retro.Columns.OfType<ActionColumn>().Single(c => c.Title == "Action Items").Id;
        setup.Context.Items.Add(new ActionItem("owner", "Mgr", [], actionId, "Nobody owns this", 0));
        await setup.Context.SaveChangesAsync();

        await mailer.NotifyAsync(await ReloadAsync(setup));

        Assert.Empty(emails.Sent);
    }

    [Fact]
    public async Task NotifyAsync_WhenSendFails_DoesNotThrow()
    {
        var setup = await SeedAsync();
        var mailer = CreateMailer(setup.Context, new ThrowingEmailSender());

        var actionId = setup.Retro.Columns.OfType<ActionColumn>().Single(c => c.Title == "Action Items").Id;
        setup.Context.Items.Add(new ActionItem("owner", "Mgr", ["Alice"], actionId, "Ship feature", 0));
        await setup.Context.SaveChangesAsync();

        await mailer.NotifyAsync(await ReloadAsync(setup));
    }

    private static ClosedRetrospectiveActionItemMailer CreateMailer(RetroDbContext context, IEmailSender emailSender) =>
        new(context, emailSender, NullLogger<ClosedRetrospectiveActionItemMailer>.Instance);

    private static Task<Retrospective> ReloadAsync(Setup setup) =>
        setup.Context.Retrospectives
            .Include(retro => retro.Columns)
            .ThenInclude(column => column.Items)
            .SingleAsync(retro => retro.Id == setup.Retro.Id);

    private static async Task<Setup> SeedAsync()
    {
        var context = new RetroDbContext(new DbContextOptionsBuilder<RetroDbContext>()
            .UseInMemoryDatabase($"close-mailer-{Guid.NewGuid()}")
            .Options);

        var organization = new Organization { Name = "Acme" };
        var alice = new AppUser("alice@acme.test", "Alice") { Id = "alice", OrganizationId = organization.Id };
        var bob = new AppUser("bob@acme.test", "Bob") { Id = "bob", OrganizationId = organization.Id };
        var carol = new AppUser("carol@acme.test", "Carol") { Id = "carol", OrganizationId = organization.Id };
        var dave = new AppUser("dave@acme.test", "Dave") { Id = "dave", OrganizationId = organization.Id };
        var retro = Retrospective.CreateNew("owner", "Sprint 11", organization.Id);
        retro.Reveal();

        context.AddRange(organization, alice, bob, carol, dave, retro);
        context.UserRetrospectives.AddRange(
            new UserRetrospective { UserId = alice.Id, RetrospectiveId = retro.Id },
            new UserRetrospective { UserId = bob.Id, RetrospectiveId = retro.Id },
            new UserRetrospective { UserId = carol.Id, RetrospectiveId = retro.Id });
        await context.SaveChangesAsync();

        return new Setup(context, retro);
    }

    private sealed record Setup(RetroDbContext Context, Retrospective Retro);

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string HtmlBody)> Sent { get; } = [];

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("SMTP unavailable");
    }
}
