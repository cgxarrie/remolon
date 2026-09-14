namespace RetroBackend.Services;

public sealed record EmailWorkItem(string To, string Subject, string HtmlBody);

public interface IBackgroundEmailQueue
{
    ValueTask EnqueueAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>Blocks until the next queued email is available, or null when the queue is completed.</summary>
    ValueTask<EmailWorkItem?> DequeueAsync(CancellationToken cancellationToken);
}
