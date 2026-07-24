using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RetroBackend.Data;

/// <summary>
/// Used by EF Core CLI tools (dotnet ef migrations add) at design time.
/// Provides a DbContext without needing the full app startup.
/// </summary>
public class RetroDbContextFactory : IDesignTimeDbContextFactory<RetroDbContext>
{
    public RetroDbContext CreateDbContext(string[] args)
    {
        var connectionString = Config.Config.PGConnectionString;

        DbContextOptionsBuilder<RetroDbContext> dbContextOptionsBuilder = new();
        var options = dbContextOptionsBuilder
            .UseNpgsql(connectionString)
            .Options;

        return new RetroDbContext(options);
    }
}
