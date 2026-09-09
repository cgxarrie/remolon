using System.Net;

namespace RetroBackend.Services;

public static class ActionItemClosedEmail
{
    public static string PendingSubject(string retrospectiveTitle) =>
        $"Pending action item from {retrospectiveTitle}";

    public static string ActionItemSubject(string retrospectiveTitle) =>
        $"Action item from {retrospectiveTitle}";

    public static string HtmlBody(
        string retrospectiveTitle,
        string kind,
        string description,
        int sessionCount,
        IEnumerable<string> assignees)
    {
        var safeTitle = WebUtility.HtmlEncode(retrospectiveTitle);
        var safeKind = WebUtility.HtmlEncode(kind);
        var safeDescription = WebUtility.HtmlEncode(description).Replace("\n", "<br />", StringComparison.Ordinal);
        var assigneeList = string.Join(", ", assignees.Select(WebUtility.HtmlEncode));
        if (string.IsNullOrWhiteSpace(assigneeList))
            assigneeList = "—";

        return $"""
            <p>A retrospective has been closed.</p>
            <p><strong>Retrospective:</strong> {safeTitle}</p>
            <p><strong>Type:</strong> {safeKind}</p>
            <p><strong>Description:</strong> {safeDescription}</p>
            <p><strong>Sessions:</strong> {sessionCount}</p>
            <p><strong>Assignees:</strong> {assigneeList}</p>
            """;
    }
}
