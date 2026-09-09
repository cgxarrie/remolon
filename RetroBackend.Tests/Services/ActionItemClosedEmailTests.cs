using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class ActionItemClosedEmailTests
{
    [Fact]
    public void HtmlBody_HtmlEncodesTitleDescriptionAndAssignees()
    {
        var html = ActionItemClosedEmail.HtmlBody(
            "Sprint <1>",
            "Action item",
            "Fix <bugs>\nand more",
            3,
            ["Alice & Bob", "all"]);

        Assert.Contains("Sprint &lt;1&gt;", html);
        Assert.Contains("Fix &lt;bugs&gt;<br />and more", html);
        Assert.Contains("Alice &amp; Bob, all", html);
        Assert.Contains("<strong>Sessions:</strong> 3", html);
        Assert.DoesNotContain("Fix <bugs>", html);
    }

    [Fact]
    public void Subjects_IncludeRetrospectiveTitle()
    {
        Assert.Equal("Pending action item from Sprint 11", ActionItemClosedEmail.PendingSubject("Sprint 11"));
        Assert.Equal("Action item from Sprint 11", ActionItemClosedEmail.ActionItemSubject("Sprint 11"));
    }
}
