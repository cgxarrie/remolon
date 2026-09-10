using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RetroBackend.Config;
using Xunit;

namespace RetroBackend.Tests.Config;

public class AllowedHostsOverrideTests
{
    [Fact]
    public void Resolve_SplitsAndTrimsHosts()
    {
        var hosts = AllowedHostsOverride.Resolve(
            Config("backend-production.up.railway.app; localhost ;127.0.0.1;"));

        Assert.NotNull(hosts);
        Assert.Equal(
            ["backend-production.up.railway.app", "localhost", "127.0.0.1"],
            hosts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(";;")]
    public void Resolve_ReturnsNull_WhenNotConfigured(string? value)
    {
        Assert.Null(AllowedHostsOverride.Resolve(Config(value)));
    }

    [Fact]
    public void Override_TakesPrecedenceOverAppSettings()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production",
            Args = [],
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "localhost;127.0.0.1",
            [AllowedHostsOverride.EnvironmentKey] = "backend-production.up.railway.app",
        });

        var hosts = AllowedHostsOverride.Resolve(builder.Configuration);
        Assert.NotNull(hosts);
        builder.Services.Configure<HostFilteringOptions>(options => options.AllowedHosts = hosts);

        using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<HostFilteringOptions>>().Value;

        Assert.Equal(["backend-production.up.railway.app"], options.AllowedHosts);
    }

    private static IConfiguration Config(string? value) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AllowedHostsOverride.EnvironmentKey] = value,
            })
            .Build();
}
