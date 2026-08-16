using Microsoft.EntityFrameworkCore;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Infrastructure.Persistence;

public sealed class OncoBridgeDbContext(DbContextOptions<OncoBridgeDbContext> options) : DbContext(options)
{
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    public DbSet<SourceResource> SourceResources => Set<SourceResource>();

    public DbSet<Lineage> Lineages => Set<Lineage>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<PrimaryCancerDiagnosis> PrimaryCancerDiagnoses => Set<PrimaryCancerDiagnosis>();

    public DbSet<CancerStaging> CancerStagings => Set<CancerStaging>();

    public DbSet<CancerSurgicalProcedure> CancerSurgicalProcedures => Set<CancerSurgicalProcedure>();

    public DbSet<Finding> Findings => Set<Finding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OncoBridgeDbContext).Assembly);
}
