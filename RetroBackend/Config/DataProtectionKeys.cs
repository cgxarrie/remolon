using Microsoft.AspNetCore.DataProtection;

namespace RetroBackend.Config;

public static class DataProtectionKeys
{
    public const string ApplicationName = "ReMolon";
    public const string ConfigurationKey = "DataProtection:KeysDirectory";
    public const string DevelopmentDirectoryName = ".dataprotection-keys";

    public static string ResolveDirectory(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration[ConfigurationKey];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured, environment.ContentRootPath);

        if (environment.IsDevelopment())
            return Path.Combine(environment.ContentRootPath, DevelopmentDirectoryName);

        throw new InvalidOperationException(
            "DataProtection:KeysDirectory is missing. Set DataProtection__KeysDirectory to a writable directory "
            + "so password-reset and invitation tokens survive restarts and work across API replicas. "
            + "Deleting or rotating those keys invalidates outstanding reset and invite links.");
    }

    public static void EnsureDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        RestrictToOwnerIfUnix(directory);
    }

    public static void AddPersisted(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var directory = ResolveDirectory(configuration, environment);
        EnsureDirectory(directory);
        services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(directory));
    }

    private static void RestrictToOwnerIfUnix(string directory)
    {
        if (OperatingSystem.IsWindows())
            return;

        File.SetUnixFileMode(
            directory,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
}
