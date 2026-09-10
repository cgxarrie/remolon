using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RetroBackend.Config;

namespace RetroBackend.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage
        {
            From = CreateFrom(_options.From),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 15);

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = (int)timeout.TotalMilliseconds,
        };

        if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
            client.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);

        // SmtpClient.Timeout does not apply to the async path, so cancellation has to enforce it.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);

        try
        {
            await client.SendMailAsync(message, deadline.Token);
        }
        // A cancelled send surfaces as SmtpException rather than OperationCanceledException,
        // so the deadline has to be checked before classifying the failure.
        catch (Exception ex) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new SmtpException($"{Describe()} did not respond within {timeout.TotalSeconds:0}s.", ex);
        }
        catch (SmtpException ex)
        {
            throw new SmtpException($"{Describe()} failed.", ex);
        }

        _logger.LogInformation(
            "Sent email '{Subject}' to {To} via {SmtpHost}:{SmtpPort}",
            subject,
            to,
            _options.SmtpHost,
            _options.SmtpPort);
    }

    private string Describe() =>
        $"SMTP send to {_options.SmtpHost}:{_options.SmtpPort} "
            + $"(EnableSsl={_options.EnableSsl}, user={(string.IsNullOrWhiteSpace(_options.SmtpUser) ? "<none>" : _options.SmtpUser)})";

    public static MailAddress CreateFrom(string from)
    {
        var parsed = new MailAddress(from);
        return string.IsNullOrWhiteSpace(parsed.DisplayName)
            ? new MailAddress(parsed.Address, "ReMolon")
            : parsed;
    }
}
