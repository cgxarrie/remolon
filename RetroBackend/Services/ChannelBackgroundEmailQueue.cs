using System.Threading.Channels;

namespace RetroBackend.Services;

public sealed class ChannelBackgroundEmailQueue : IBackgroundEmailQueue
{
    private readonly Channel<EmailWorkItem> _channel = Channel.CreateUnbounded<EmailWorkItem>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public ValueTask EnqueueAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(new EmailWorkItem(to, subject, htmlBody), cancellationToken);

    public async ValueTask<EmailWorkItem?> DequeueAsync(CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_channel.Reader.TryRead(out var item))
                return item;
        }

        return null;
    }
}
