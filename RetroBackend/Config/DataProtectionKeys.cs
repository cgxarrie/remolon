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
        VerifyWritable(directory);
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

        // A mounted volume is often owned by another user, so tightening the mode
        // is not possible. That is not fatal as long as the keys remain writable.
        try
        {
            File.SetUnixFileMode(
                directory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static void VerifyWritable(string directory)
    {
        var probe = Path.Combine(directory, $".write-probe-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException(
                $"Data Protection keys directory '{directory}' is not writable. Password-reset and "
                + "invitation links need a writable directory that survives restarts. Mounted volumes "
                + "are usually owned by root, so either run the container as its owner (on Railway set "
                + "RAILWAY_RUN_UID=0) or point DataProtection__KeysDirectory at a writable path.",
                exception);
        }
    }
}
