using Microsoft.Extensions.Configuration;
using Npgsql;
using RetroBackend.Config;
using Xunit;

namespace RetroBackend.Tests.Config;

public class PostgresConnectionStringTests
{
    [Fact]
    public void Resolve_PrefersDefaultConnection()
    {
        var config = Config(
            ("ConnectionStrings:DefaultConnection", "Host=compose;Database=retrodb;Username=retro;Password=secret"),
            ("DATABASE_PRIVATE_URL", "postgresql://postgres:other@postgres.railway.internal:5432/railway"),
            ("DATABASE_URL", "postgresql://postgres:public@proxy.rlwy.net:12345/railway"));

        Assert.Equal(
            "Host=compose;Database=retrodb;Username=retro;Password=secret",
            PostgresConnectionString.Resolve(config));
    }

    [Fact]
    public void Resolve_PrefersPrivateUrlOverPublicUrl()
    {
        var config = Config(
            ("DATABASE_PRIVATE_URL", "postgresql://postgres:priv@postgres.railway.internal:5432/railway"),
            ("DATABASE_URL", "postgresql://postgres:public@maglev.proxy.rlwy.net:12345/railway"));

        var parsed = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Resolve(config));

        Assert.Equal("postgres.railway.internal", parsed.Host);
        Assert.Equal("priv", parsed.Password);
        Assert.Equal(SslMode.Disable, parsed.SslMode);
    }

    [Fact]
    public void Resolve_UsesDatabaseUrlWhenPrivateUrlMissing()
    {
        var config = Config(
            ("DATABASE_URL", "postgresql://postgres:pub%40lic@maglev.proxy.rlwy.net:12345/railway"));

        var parsed = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Resolve(config));

        Assert.Equal("maglev.proxy.rlwy.net", parsed.Host);
        Assert.Equal(12345, parsed.Port);
        Assert.Equal("railway", parsed.Database);
        Assert.Equal("postgres", parsed.Username);
        Assert.Equal("pub@lic", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void Resolve_BuildsFromPgVariables()
    {
        var config = Config(
            ("PGHOST", "postgres.railway.internal"),
            ("PGPORT", "5432"),
            ("PGDATABASE", "railway"),
            ("PGUSER", "postgres"),
            ("PGPASSWORD", "from-parts"));

        var parsed = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Resolve(config));

        Assert.Equal("postgres.railway.internal", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("railway", parsed.Database);
        Assert.Equal("postgres", parsed.Username);
        Assert.Equal("from-parts", parsed.Password);
        Assert.Equal(SslMode.Disable, parsed.SslMode);
    }

    [Fact]
    public void Resolve_ThrowsWhenNothingConfigured()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => PostgresConnectionString.Resolve(Config()));

        Assert.Contains("DATABASE_PRIVATE_URL", ex.Message, StringComparison.Ordinal);
    }

    private static IConfiguration Config(params (string Key, string? Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(pair => pair.Key, pair => pair.Value))
            .Build();
}
