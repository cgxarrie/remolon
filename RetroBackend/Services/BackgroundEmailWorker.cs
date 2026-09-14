namespace RetroBackend.Services;

public sealed class BackgroundEmailWorker : BackgroundService
{
    private readonly IBackgroundEmailQueue _queue;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<BackgroundEmailWorker> _logger;

    public BackgroundEmailWorker(
        IBackgroundEmailQueue queue,
        IEmailSender emailSender,
        ILogger<BackgroundEmailWorker> logger)
    {
        _queue = queue;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            EmailWorkItem? item;
            try
            {
                item = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (item is null)
                break;

            try
            {
                await _emailSender.SendAsync(item.To, item.Subject, item.HtmlBody, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Background email to {To} with subject '{Subject}' failed", item.To, item.Subject);
            }
        }
    }
}
