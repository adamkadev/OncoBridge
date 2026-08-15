using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OncoBridge.Infrastructure.Persistence;

internal sealed class OncoBridgeDbContextFactory : IDesignTimeDbContextFactory<OncoBridgeDbContext>
{
    private const string ConnectionStringVariable = "ONCOBRIDGE_DESIGN_TIME_CONNECTION";

    public OncoBridgeDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? "Host=localhost;Database=oncobridge_design_time;Username=postgres;Password=postgres";

        DbContextOptions<OncoBridgeDbContext> options =
            new DbContextOptionsBuilder<OncoBridgeDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        return new OncoBridgeDbContext(options);
    }
}
