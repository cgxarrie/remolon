using Microsoft.Extensions.Configuration;
using RetroBackend.Config;
using Xunit;

namespace RetroBackend.Tests.Config;

public class JwtSigningKeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_Throws_WhenKeyMissing(string? key)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSigningKey.Resolve(Config(key)));
        Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_Throws_WhenKeyTooShort()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => JwtSigningKey.Resolve(Config("short-key-not-32-chars")));
        Assert.Contains("at least 32", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_Throws_WhenKeyIsPlaceholder()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => JwtSigningKey.Resolve(Config(JwtSigningKey.Placeholder)));
        Assert.Contains("placeholder", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_ReturnsKey_WhenValid()
    {
        const string key = "unit-test-jwt-signing-key-32ch!!";
        Assert.Equal(32, key.Length);
        Assert.Equal(key, JwtSigningKey.Resolve(Config(key)));
    }

    private static IConfiguration Config(string? key) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = key,
            })
            .Build();
}
