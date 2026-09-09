using Microsoft.EntityFrameworkCore;
using RetroBackend.Data;
using RetroBackend.Models;

namespace RetroBackend.Services;

public class ClosedRetrospectiveActionItemMailer : IClosedRetrospectiveActionItemMailer
{
    public const string AllAssignees = "all";
    private const string PendingKind = "Pending action item";
    private const string ActionKind = "Action item";

    private readonly RetroDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ClosedRetrospectiveActionItemMailer> _logger;

    public ClosedRetrospectiveActionItemMailer(
        RetroDbContext context,
        IEmailSender emailSender,
        ILogger<ClosedRetrospectiveActionItemMailer> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task NotifyAsync(Retrospective retrospective, CancellationToken cancellationToken = default)
    {
        var participants = await _context.UserRetrospectives
            .Include(assignment => assignment.User)
            .Where(assignment => assignment.RetrospectiveId == retrospective.Id)
            .Select(assignment => assignment.User)
            .Where(user => user != null)
            .ToListAsync(cancellationToken);

        var orgUsers = await _context.Users
            .Where(user => user.OrganizationId == retrospective.OrganizationId)
            .ToListAsync(cancellationToken);

        var pendingItems = retrospective.Columns
            .OfType<ActionColumn>()
            .Where(column => column.Title == "Pending Action Items")
            .SelectMany(column => column.Items.OfType<ActionItem>())
            .Where(item => !item.IsCompleted);

        var actionItems = retrospective.Columns
            .OfType<ActionColumn>()
            .Where(column => column.Title == "Action Items")
            .SelectMany(column => column.Items.OfType<ActionItem>());

        foreach (var item in pendingItems)
        {
            await SendItemAsync(
                retrospective,
                item,
                PendingKind,
                ActionItemClosedEmail.PendingSubject(retrospective.Title),
                participants,
                orgUsers,
                cancellationToken);
        }

        foreach (var item in actionItems)
        {
            await SendItemAsync(
                retrospective,
                item,
                ActionKind,
                ActionItemClosedEmail.ActionItemSubject(retrospective.Title),
                participants,
                orgUsers,
                cancellationToken);
        }
    }

    private async Task SendItemAsync(
        Retrospective retrospective,
        ActionItem item,
        string kind,
        string subject,
        IReadOnlyList<AppUser> participants,
        IReadOnlyList<AppUser> orgUsers,
        CancellationToken cancellationToken)
    {
        var recipients = ResolveRecipientEmails(item.Assignees, participants, orgUsers);
        if (recipients.Count == 0)
            return;

        var sessionCount = item.Iterations > 0 ? item.Iterations : 1;
        var html = ActionItemClosedEmail.HtmlBody(
            retrospective.Title,
            kind,
            item.Description,
            sessionCount,
            item.Assignees);

        foreach (var email in recipients)
        {
            try
            {
                await _emailSender.SendAsync(email, subject, html, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send closed action item email for retrospective {RetrospectiveId} to {Email}",
                    retrospective.Id,
                    email);
            }
        }
    }

    private static IReadOnlyList<string> ResolveRecipientEmails(
        IReadOnlyList<string> assignees,
        IReadOnlyList<AppUser> participants,
        IReadOnlyList<AppUser> orgUsers)
    {
        if (assignees.Any(assignee => assignee.Equals(AllAssignees, StringComparison.OrdinalIgnoreCase)))
        {
            return participants
                .Select(user => user.Email)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var usersByNickname = participants
            .Concat(orgUsers)
            .GroupBy(user => user.Nickname, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        return assignees
            .Select(nickname => usersByNickname.TryGetValue(nickname, out var user) ? user.Email : null)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
