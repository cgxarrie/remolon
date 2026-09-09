using Microsoft.Extensions.Configuration;

namespace RetroBackend.Tests.Config;

public static class TestJwt
{
    public const string Key = "unit-test-jwt-signing-key-32ch!!";

    public static IConfiguration Configuration { get; } =
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Key,
                ["Jwt:Issuer"] = "RetroBackend",
                ["Jwt:Audience"] = "RetroBackendClients",
            })
            .Build();
}
