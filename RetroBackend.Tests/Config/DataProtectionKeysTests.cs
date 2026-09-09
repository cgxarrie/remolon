using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RetroBackend.Config;
using Xunit;

namespace RetroBackend.Tests.Config;

public class DataProtectionKeysTests
{
    [Fact]
    public void ResolveDirectory_UsesConfiguredPathRelativeToContentRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var env = Env(Environments.Production, root);

        var directory = DataProtectionKeys.ResolveDirectory(Config("keys"), env);

        Assert.Equal(Path.Combine(root, "keys"), directory);
    }

    [Fact]
    public void ResolveDirectory_DefaultsInDevelopmentWhenUnconfigured()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var env = Env(Environments.Development, root);

        var directory = DataProtectionKeys.ResolveDirectory(Config(null), env);

        Assert.Equal(Path.Combine(root, DataProtectionKeys.DevelopmentDirectoryName), directory);
    }

    [Fact]
    public void ResolveDirectory_ThrowsInProductionWhenUnconfigured()
    {
        var env = Env(Environments.Production, Path.GetTempPath());

        var ex = Assert.Throws<InvalidOperationException>(
            () => DataProtectionKeys.ResolveDirectory(Config(null), env));

        Assert.Contains("KeysDirectory", ex.Message, StringComparison.Ordinal);
        Assert.Contains("invalidates", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddPersisted_ProtectsPayloadAfterNewServiceProvider()
    {
        var root = Path.Combine(Path.GetTempPath(), $"dp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var env = Env(Environments.Production, root);
            var configuration = Config(Path.Combine(root, "keys"));

            var first = new ServiceCollection();
            DataProtectionKeys.AddPersisted(first, configuration, env);
            using var firstProvider = first.BuildServiceProvider();
            var protectedPayload = firstProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("invite-or-reset")
                .Protect("token-payload");

            var second = new ServiceCollection();
            DataProtectionKeys.AddPersisted(second, configuration, env);
            using var secondProvider = second.BuildServiceProvider();
            var unprotected = secondProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("invite-or-reset")
                .Unprotect(protectedPayload);

            Assert.Equal("token-payload", unprotected);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static IConfiguration Config(string? keysDirectory) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DataProtectionKeys.ConfigurationKey] = keysDirectory,
            })
            .Build();

    private static IHostEnvironment Env(string environmentName, string contentRoot) =>
        new StubHostEnvironment
        {
            EnvironmentName = environmentName,
            ContentRootPath = contentRoot,
            ApplicationName = "test",
            ContentRootFileProvider = new NullFileProvider(),
        };

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
