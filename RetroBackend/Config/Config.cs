namespace RetroBackend.Config;

internal static class Config
{
    private static readonly Lazy<IConfigurationRoot> _instance = new(() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build()
    );


    public static string PGConnectionString => _instance.Value.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}