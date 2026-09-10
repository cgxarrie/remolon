using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RetroBackend.Config;
using RetroBackend.Services;
using Xunit;

namespace RetroBackend.Tests.Services;

public class ResendEmailSenderTests
{
    [Fact]
    public async Task SendAsync_PostsJsonToResendWithBearerKey()
    {
        var handler = new StubHandler();
        var sender = CreateSender(handler, apiKey: "re_test_key", from: "noreply@example.com");

        await sender.SendAsync("user@example.com", "Set your password", "<p>Hello</p>");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/emails", handler.LastRequest.RequestUri?.AbsolutePath);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal("re_test_key", handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Contains("\"from\":\"ReMolon <noreply@example.com>\"", handler.LastBody);
        Assert.Contains("\"to\":[\"user@example.com\"]", handler.LastBody);
        Assert.Contains("\"subject\":\"Set your password\"", handler.LastBody);
        Assert.Contains("\"html\":\"<p>Hello</p>\"", handler.LastBody);
    }

    [Fact]
    public async Task SendAsync_KeepsDisplayNameWhenFromAlreadyHasOne()
    {
        var handler = new StubHandler();
        var sender = CreateSender(handler, apiKey: "re_test_key", from: "ReMolon <hello@example.com>");

        await sender.SendAsync("user@example.com", "Hi", "<p>x</p>");

        Assert.Contains("\"from\":\"ReMolon <hello@example.com>\"", handler.LastBody);
    }

    [Fact]
    public async Task SendAsync_ThrowsWhenApiKeyMissing()
    {
        var sender = CreateSender(new StubHandler(), apiKey: " ", from: "noreply@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync("user@example.com", "Hi", "<p>x</p>"));
    }

    [Fact]
    public async Task SendAsync_ThrowsWhenResendReturnsError()
    {
        var handler = new StubHandler
        {
            StatusCode = HttpStatusCode.UnprocessableEntity,
            ResponseBody = """{"statusCode":422,"message":"Invalid from"}""",
        };
        var sender = CreateSender(handler, apiKey: "re_test_key", from: "noreply@example.com");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sender.SendAsync("user@example.com", "Hi", "<p>x</p>"));
    }

    private static ResendEmailSender CreateSender(StubHandler handler, string apiKey, string from)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var options = Options.Create(new EmailOptions
        {
            Provider = "resend",
            ApiKey = apiKey,
            From = from,
        });
        return new ResendEmailSender(http, options, NullLogger<ResendEmailSender>.Instance);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseBody { get; set; } = """{"id":"49a3999c-0ce1-4ea6-ab68-afcd6dc2e794"}""";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
