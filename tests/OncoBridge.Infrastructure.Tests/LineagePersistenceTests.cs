using Microsoft.EntityFrameworkCore;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Infrastructure.Persistence;
using OncoBridge.Interop.Fhir.Ingestion;

namespace OncoBridge.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class LineagePersistenceTests(PostgreSqlFixture postgres)
{
    private static readonly DateTimeOffset ReceivedAt = new(2026, 8, 15, 9, 30, 0, TimeSpan.Zero);

    private async Task<(OncoBridgeDbContext Context, SourceResource Source)> SeedSourceResourceAsync()
    {
        OncoBridgeDbContext context = await postgres.CreateMigratedContextAsync();

        IngestedBundle ingested = new FhirBundleIngestor()
            .Ingest(SyntheticFixtures.MinimalBundleBytes, "phase2-fixture", ReceivedAt);

        await new ImportBatchStore(context).SaveAsync(ingested.Batch, ingested.SourceResources);
        context.ChangeTracker.Clear();

        return (context, ingested.SourceResources[0]);
    }

    [Fact]
    public async Task Ingestion_produces_no_lineage()
    {
        (OncoBridgeDbContext context, _) = await SeedSourceResourceAsync();
        await using OncoBridgeDbContext _context = context;

        Assert.Equal(0, await context.Lineages.CountAsync());
    }

    [Fact]
    public async Task Entity_level_lineage_round_trips_with_a_null_field_path()
    {
        (OncoBridgeDbContext context, SourceResource source) = await SeedSourceResourceAsync();
        await using OncoBridgeDbContext _context = context;

        Guid entityId = Guid.NewGuid();
        context.Lineages.Add(Lineage.ForEntity(
            "CancerStaging", entityId, source.Id, "TestTransformation", "1.0.0"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Lineage reloaded = await context.Lineages.SingleAsync();

        Assert.Null(reloaded.FieldPath);
        Assert.True(reloaded.IsWholeEntity);
        Assert.Equal(entityId, reloaded.DomainEntityId);
        Assert.Equal("1.0.0", reloaded.TransformationVersion);
    }

    [Fact]
    public async Task Field_level_lineage_round_trips_with_its_field_path()
    {
        (OncoBridgeDbContext context, SourceResource source) = await SeedSourceResourceAsync();
        await using OncoBridgeDbContext _context = context;

        context.Lineages.Add(Lineage.ForField(
            "CancerStaging", Guid.NewGuid(), "Categories[T]", source.Id, "TestTransformation", "1.0.0"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Lineage reloaded = await context.Lineages.SingleAsync();

        Assert.Equal("Categories[T]", reloaded.FieldPath);
        Assert.False(reloaded.IsWholeEntity);
    }

    [Fact]
    public async Task Lineage_cannot_reference_a_source_resource_that_does_not_exist()
    {
        (OncoBridgeDbContext context, _) = await SeedSourceResourceAsync();
        await using OncoBridgeDbContext _context = context;

        context.Lineages.Add(Lineage.ForEntity(
            "CancerStaging", Guid.NewGuid(), SourceResourceId.New(), "TestTransformation", "1.0.0"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Several_lineage_rows_may_point_at_the_same_domain_entity()
    {
        (OncoBridgeDbContext context, SourceResource source) = await SeedSourceResourceAsync();
        await using OncoBridgeDbContext _context = context;

        Guid entityId = Guid.NewGuid();
        context.Lineages.AddRange(
            Lineage.ForField("CancerStaging", entityId, "Categories[T]", source.Id, "T", "1.0.0"),
            Lineage.ForField("CancerStaging", entityId, "Categories[N]", source.Id, "T", "1.0.0"),
            Lineage.ForField("CancerStaging", entityId, "Categories[M]", source.Id, "T", "1.0.0"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(3, await context.Lineages.CountAsync(l => l.DomainEntityId == entityId));
    }
}
