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

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
            client.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException ex)
        {
            throw new SmtpException(
                $"SMTP send to {_options.SmtpHost}:{_options.SmtpPort} failed "
                    + $"(EnableSsl={_options.EnableSsl}, user={(string.IsNullOrWhiteSpace(_options.SmtpUser) ? "<none>" : _options.SmtpUser)}).",
                ex);
        }

        _logger.LogInformation(
            "Sent email '{Subject}' to {To} via {SmtpHost}:{SmtpPort}",
            subject,
            to,
            _options.SmtpHost,
            _options.SmtpPort);
    }

    public static MailAddress CreateFrom(string from)
    {
        var parsed = new MailAddress(from);
        return string.IsNullOrWhiteSpace(parsed.DisplayName)
            ? new MailAddress(parsed.Address, "ReMolon")
            : parsed;
    }
}
