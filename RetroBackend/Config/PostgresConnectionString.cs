using Npgsql;

namespace RetroBackend.Config;

public static class PostgresConnectionString
{
    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        var privateUrl = configuration["DATABASE_PRIVATE_URL"];
        if (!string.IsNullOrWhiteSpace(privateUrl))
            return FromRailwayValue(privateUrl);

        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return FromRailwayValue(databaseUrl);

        var fromParts = FromPgEnvironment(configuration);
        if (fromParts is not null)
            return fromParts;

        throw new InvalidOperationException(
            "PostgreSQL connection string is missing. Set ConnectionStrings__DefaultConnection, "
            + "or Railway DATABASE_PRIVATE_URL / DATABASE_URL, "
            + "or PGHOST, PGPORT, PGDATABASE, PGUSER, and PGPASSWORD.");
    }

    private static string? FromPgEnvironment(IConfiguration configuration)
    {
        var host = configuration["PGHOST"];
        var user = configuration["PGUSER"];
        var password = configuration["PGPASSWORD"];
        var database = configuration["PGDATABASE"];
        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(user)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(database))
        {
            return null;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Username = user,
            Password = password,
            Database = database,
        };

        if (int.TryParse(configuration["PGPORT"], out var port) && port > 0)
            builder.Port = port;

        ApplySsl(builder);
        return builder.ConnectionString;
    }

    private static string FromRailwayValue(string value)
    {
        var builder = Parse(value.Trim());
        ApplySsl(builder);
        return builder.ConnectionString;
    }

    private static NpgsqlConnectionStringBuilder Parse(string value)
    {
        if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(value);
            var userInfo = uri.UserInfo.Split(':', 2);
            var database = uri.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(uri.Host) || string.IsNullOrWhiteSpace(database))
            {
                throw new InvalidOperationException(
                    "Railway DATABASE_URL is not a valid PostgreSQL URI.");
            }

            return new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort ? 5432 : uri.Port,
                Database = Uri.UnescapeDataString(database),
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
            };
        }

        return new NpgsqlConnectionStringBuilder(value);
    }

    private static void ApplySsl(NpgsqlConnectionStringBuilder builder)
    {
        var host = builder.Host ?? "";
        if (host.EndsWith(".railway.internal", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = SslMode.Disable;
            return;
        }

        builder.SslMode = SslMode.Require;
    }
}
