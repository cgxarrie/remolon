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
            From = new MailAddress(_options.From, "ReMolon"),
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

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Sent email '{Subject}' to {To}", subject, to);
    }
}
