using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class UserInvitationEmailTests
{
    [Fact]
    public void HtmlBody_HtmlEncodesEmailAndUrlAndDoesNotIncludeAPassword()
    {
        var html = UserInvitationEmail.HtmlBody(
            "a&b@example.com",
            "http://localhost:3000/reset-password#email=a&token=b\"onclick=x");

        Assert.Contains("valid for 30 days", html);
        Assert.Contains("token=b&quot;onclick=x", html);
        Assert.DoesNotContain("Temporary password", html);
        Assert.DoesNotContain("token=b\"onclick=x", html);
    }
}
