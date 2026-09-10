using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RetroBackend.Config;

namespace RetroBackend.Services;

public class ResendEmailSender : IEmailSender
{
    internal const string ApiBaseAddress = "https://api.resend.com/";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly HttpClient _http;
    private readonly EmailOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient http, IOptions<EmailOptions> options, ILogger<ResendEmailSender> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Email:ApiKey is required when Email:Provider is resend.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            from = FormatFrom(_options.From),
            to = new[] { to },
            subject,
            html = htmlBody,
        }, options: JsonOptions);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Resend rejected email '{Subject}' to {To}: {Status} {Body}",
                subject,
                to,
                (int)response.StatusCode,
                body);
            throw new HttpRequestException($"Resend returned {(int)response.StatusCode}.");
        }

        _logger.LogInformation("Sent email '{Subject}' to {To}", subject, to);
    }

    internal static string FormatFrom(string from)
    {
        if (from.Contains('<', StringComparison.Ordinal))
            return from;
        return $"ReMolon <{from}>";
    }
}
