using RetroBackend.Services;

namespace RetroBackend.Tests.Fakes;

/// <summary>Runs enqueued emails inline — keeps existing controller tests deterministic.</summary>
public sealed class ImmediateBackgroundEmailQueue : IBackgroundEmailQueue
{
    private readonly IEmailSender _emailSender;

    public ImmediateBackgroundEmailQueue(IEmailSender emailSender) => _emailSender = emailSender;

    public async ValueTask EnqueueAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        await _emailSender.SendAsync(to, subject, htmlBody, cancellationToken);

    public ValueTask<EmailWorkItem?> DequeueAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult<EmailWorkItem?>(null);
}

/// <summary>Stores work items without sending — proves the request path does not await SMTP.</summary>
public sealed class CapturingBackgroundEmailQueue : IBackgroundEmailQueue
{
    public List<EmailWorkItem> Items { get; } = [];

    public ValueTask EnqueueAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        Items.Add(new EmailWorkItem(to, subject, htmlBody));
        return ValueTask.CompletedTask;
    }

    public ValueTask<EmailWorkItem?> DequeueAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult<EmailWorkItem?>(null);
}

/// <summary>No-op queue for controllers that do not exercise forgot-password.</summary>
public sealed class NoOpBackgroundEmailQueue : IBackgroundEmailQueue
{
    public ValueTask EnqueueAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    public ValueTask<EmailWorkItem?> DequeueAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult<EmailWorkItem?>(null);
}
