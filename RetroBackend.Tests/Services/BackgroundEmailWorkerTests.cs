using Microsoft.Extensions.Logging.Abstractions;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class BackgroundEmailWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_SendsQueuedItemsAndSwallowsSenderFailures()
    {
        var queue = new ChannelBackgroundEmailQueue();
        var sender = new RecordingEmailSender { FailOn = "bad@example.com" };
        using var cts = new CancellationTokenSource();
        using var worker = new BackgroundEmailWorker(queue, sender, NullLogger<BackgroundEmailWorker>.Instance);

        var run = worker.StartAsync(cts.Token);
        await queue.EnqueueAsync("ok@example.com", "Hello", "<p>Hi</p>");
        await queue.EnqueueAsync("bad@example.com", "Nope", "<p>Fail</p>");

        await WaitForAsync(() => sender.Sent.Count >= 1 && sender.Attempted.Count >= 2);
        cts.Cancel();
        await run;

        var sent = Assert.Single(sender.Sent);
        Assert.Equal("ok@example.com", sent.To);
        Assert.Contains(sender.Attempted, to => to == "bad@example.com");
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (condition())
                return;
            await Task.Delay(20);
        }

        Assert.Fail("Timed out waiting for background worker.");
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public string? FailOn { get; init; }
        public List<(string To, string Subject, string HtmlBody)> Sent { get; } = [];
        public List<string> Attempted { get; } = [];

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            Attempted.Add(to);
            if (FailOn is not null && string.Equals(to, FailOn, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SMTP unavailable");
            Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }
}
