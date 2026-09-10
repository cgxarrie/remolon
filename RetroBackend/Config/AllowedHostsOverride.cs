namespace RetroBackend.Config;

/// <summary>
/// ASPNETCORE_ALLOWEDHOSTS lands in host configuration, which appsettings.json
/// overrides, so the value has to be applied to host filtering explicitly.
/// </summary>
public static class AllowedHostsOverride
{
    public const string EnvironmentKey = "ASPNETCORE_ALLOWEDHOSTS";

    public static string[]? Resolve(IConfiguration configuration)
    {
        var configured = configuration[EnvironmentKey];
        if (string.IsNullOrWhiteSpace(configured))
            return null;

        var hosts = configured.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return hosts.Length > 0 ? hosts : null;
    }
}
