using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RetroBackend.Config;
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

    [Fact]
    public async Task SendAsync_GivesUpWhenServerAcceptsButNeverGreets()
    {
        // Listening without accepting mimics a firewalled SMTP port: the send would
        // otherwise block until the reverse proxy gives up and returns 504.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        try
        {
            var sender = new SmtpEmailSender(
                Options.Create(new EmailOptions
                {
                    SmtpHost = "127.0.0.1",
                    SmtpPort = port,
                    From = "noreply@example.com",
                    TimeoutSeconds = 1,
                }),
                NullLogger<SmtpEmailSender>.Instance);

            var elapsed = Stopwatch.StartNew();
            var ex = await Assert.ThrowsAsync<SmtpException>(() =>
                sender.SendAsync("user@example.com", "Subject", "<p>Body</p>"));
            elapsed.Stop();

            Assert.Contains($"127.0.0.1:{port}", ex.Message);
            Assert.Contains("did not respond within 1s", ex.Message);
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(15), $"Took {elapsed.Elapsed}.");
        }
        finally
        {
            listener.Stop();
        }
    }
}
