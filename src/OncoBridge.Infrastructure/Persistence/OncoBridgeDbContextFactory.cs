using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OncoBridge.Infrastructure.Persistence;

internal sealed class OncoBridgeDbContextFactory : IDesignTimeDbContextFactory<OncoBridgeDbContext>
{
    public OncoBridgeDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("ONCOBRIDGE_DESIGN_TIME_CONNECTION")
            ?? "Host=localhost;Database=oncobridge_design_time;Username=postgres;Password=postgres";

        DbContextOptions<OncoBridgeDbContext> options =
            new DbContextOptionsBuilder<OncoBridgeDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        return new OncoBridgeDbContext(options);
    }
}
