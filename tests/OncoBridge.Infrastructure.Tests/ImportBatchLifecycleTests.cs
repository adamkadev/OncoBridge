using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Infrastructure.Persistence;

namespace OncoBridge.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ImportBatchLifecycleTests(PostgreSqlFixture postgres)
{
    [Fact]
    public async Task An_ingested_batch_is_received_and_carries_no_normalization_metadata()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatch batch = await scenario.ReloadBatchAsync(await scenario.IngestCompleteBundleAsync());

        Assert.Equal(ImportBatchStatus.Received, batch.Status);
        Assert.Null(batch.NormalizerVersion);
        Assert.Null(batch.NormalizedAt);
    }

    [Fact]
    public async Task A_normalized_batch_records_the_pipeline_version_and_the_supplied_instant()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestCompleteBundleAsync();
        await scenario.NormalizeAsync(batchId);

        ImportBatch batch = await scenario.ReloadBatchAsync(batchId);

        Assert.Equal(ImportBatchStatus.Normalized, batch.Status);
        Assert.Equal("1.0.0", batch.NormalizerVersion);
        Assert.Equal(NormalizationScenario.NormalizedAt, batch.NormalizedAt);
    }

    [Fact]
    public async Task Normalizing_a_batch_that_does_not_exist_returns_nothing_and_writes_nothing()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        Assert.Null(await scenario.NormalizeAsync(ImportBatchId.New()));
        Assert.Equal(new CanonicalCounts(0, 0, 0, 0, 0, 0), await scenario.CountsAsync());
    }

    [Fact]
    public async Task A_batch_whose_sources_yield_no_canonical_entity_still_normalizes()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        await using OncoBridgeDbContext _context = scenario.Context;

        ImportBatchId batchId = await scenario.IngestAsync(
            SyntheticFixtures.Utf8(TemporalFixtures.NothingNormalizableBundle), "phase3d-empty");

        Assert.NotNull(await scenario.NormalizeAsync(batchId));

        ImportBatch batch = await scenario.ReloadBatchAsync(batchId);

        Assert.Equal(ImportBatchStatus.Normalized, batch.Status);
        Assert.Equal(NormalizationScenario.NormalizedAt, batch.NormalizedAt);
        Assert.Equal(new CanonicalCounts(0, 0, 0, 0, 0, 0), await scenario.CountsAsync());
    }

    [Fact]
    public void A_normalization_instant_before_receipt_is_rejected()
    {
        ImportBatch batch = ImportBatch.Create(
            ImportBatchId.New(), "phase3d", NormalizationScenario.ReceivedAt, "{}"u8);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => batch.MarkNormalized("1.0.0", NormalizationScenario.ReceivedAt.AddSeconds(-1)));
        Assert.Equal(ImportBatchStatus.Received, batch.Status);
    }

    [Fact]
    public void A_blank_normalizer_version_is_rejected()
    {
        ImportBatch batch = ImportBatch.Create(
            ImportBatchId.New(), "phase3d", NormalizationScenario.ReceivedAt, "{}"u8);

        Assert.Throws<ArgumentException>(
            () => batch.MarkNormalized("  ", NormalizationScenario.NormalizedAt));
        Assert.Null(batch.NormalizerVersion);
    }
}
