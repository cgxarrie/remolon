using RetroBackend.Auth;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class PasswordResetEmailTests
{
    [Fact]
    public void BuildResetUrl_EncodesEmailAndToken()
    {
        var url = PasswordResetEmail.BuildResetUrl(
            "http://localhost:3000/",
            "user+tag@example.com",
            "abc+def/ghi");

        Assert.Equal(
            "http://localhost:3000/reset-password#email=user%2Btag%40example.com&token=abc%2Bdef%2Fghi",
            url);
    }

    [Fact]
    public void BuildResetUrl_InviteAddsPurposeInFragment()
    {
        var url = PasswordResetEmail.BuildResetUrl(
            "http://localhost:3000/",
            "user@example.com",
            "tok",
            invite: true);

        Assert.Equal(
            "http://localhost:3000/reset-password#email=user%40example.com&token=tok&purpose=invite",
            url);
    }

    [Fact]
    public void HtmlBody_HtmlEncodesResetUrl()
    {
        var html = PasswordResetEmail.HtmlBody("http://localhost:3000/reset-password#email=a&token=b\"onclick=x");

        Assert.Contains("valid for 30 minutes", html);
        Assert.Contains("token=b&quot;onclick=x", html);
        Assert.DoesNotContain("token=b\"onclick=x", html);
    }

    [Fact]
    public void PasswordResetTokenProvider_ExpiresAfterThirtyMinutes()
    {
        var options = new PasswordResetTokenProviderOptions();

        Assert.Equal(PasswordResetTokenProviderOptions.ProviderName, options.Name);
        Assert.Equal(TimeSpan.FromMinutes(30), options.TokenLifespan);
    }

    [Fact]
    public void InvitationTokenProvider_ExpiresAfterThirtyDays()
    {
        var options = new InvitationTokenProviderOptions();

        Assert.Equal(InvitationTokenProviderOptions.ProviderName, options.Name);
        Assert.Equal(TimeSpan.FromDays(30), options.TokenLifespan);
    }
}
