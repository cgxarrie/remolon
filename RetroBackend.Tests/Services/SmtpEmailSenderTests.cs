using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class SmtpEmailSenderTests
{
    [Fact]
    public void CreateFrom_AddsDisplayNameWhenFromIsBareAddress()
    {
        var from = SmtpEmailSender.CreateFrom("noreply@example.com");

        Assert.Equal("noreply@example.com", from.Address);
        Assert.Equal("ReMolon", from.DisplayName);
    }

    [Fact]
    public void CreateFrom_KeepsDisplayNameWhenFromAlreadyHasOne()
    {
        var from = SmtpEmailSender.CreateFrom("ReMolon <hello@example.com>");

        Assert.Equal("hello@example.com", from.Address);
        Assert.Equal("ReMolon", from.DisplayName);
    }
}
