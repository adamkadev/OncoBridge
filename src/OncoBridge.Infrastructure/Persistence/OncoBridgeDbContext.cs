using Microsoft.EntityFrameworkCore;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Infrastructure.Persistence;

public sealed class OncoBridgeDbContext(DbContextOptions<OncoBridgeDbContext> options) : DbContext(options)
{
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    public DbSet<SourceResource> SourceResources => Set<SourceResource>();

    public DbSet<Lineage> Lineages => Set<Lineage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OncoBridgeDbContext).Assembly);
}
