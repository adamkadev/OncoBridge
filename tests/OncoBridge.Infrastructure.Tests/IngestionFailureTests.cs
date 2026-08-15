using Microsoft.EntityFrameworkCore;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Infrastructure.Persistence;
using OncoBridge.Interop.Fhir.Ingestion;

namespace OncoBridge.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class IngestionFailureTests(PostgreSqlFixture postgres)
{
    private static readonly DateTimeOffset ReceivedAt = new(2026, 8, 15, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_save_that_violates_a_constraint_persists_no_part_of_the_import()
    {
        await using OncoBridgeDbContext context = await postgres.CreateMigratedContextAsync();
        ImportBatchStore store = new(context);

        IngestedBundle ingested = new FhirBundleIngestor()
            .Ingest(SyntheticFixtures.MinimalBundleBytes, "phase2-fixture", ReceivedAt);

        SourceResource[] conflicting =
        [
            .. ingested.SourceResources,
            new SourceResource(SourceResourceId.New(), ingested.Batch.Id, entryIndex: 0),
        ];

        await Assert.ThrowsAsync<DbUpdateException>(
            () => store.SaveAsync(ingested.Batch, conflicting));

        context.ChangeTracker.Clear();

        Assert.Equal(0, await store.CountBatchesAsync());
        Assert.Empty(await store.GetSourceResourcesAsync(ingested.Batch.Id));
    }

    [Fact]
    public async Task Input_that_is_not_a_bundle_fails_before_anything_is_persisted()
    {
        await using OncoBridgeDbContext context = await postgres.CreateMigratedContextAsync();
        ImportBatchStore store = new(context);

        Assert.Throws<BundleIngestionException>(() => new FhirBundleIngestor()
            .Ingest(SyntheticFixtures.Utf8("""{"resourceType":"Patient"}"""), "phase2-fixture", ReceivedAt));

        Assert.Equal(0, await store.CountBatchesAsync());
    }

    [Fact]
    public async Task A_reserialised_payload_has_a_different_digest_despite_identical_meaning()
    {
        await using OncoBridgeDbContext context = await postgres.CreateMigratedContextAsync();
        ImportBatchStore store = new(context);
        FhirBundleIngestor ingestor = new();

        IngestedBundle original = ingestor.Ingest(
            SyntheticFixtures.MinimalBundleBytes, "phase2-fixture", ReceivedAt);
        IngestedBundle reserialised = ingestor.Ingest(
            SyntheticFixtures.MinimalBundleReserialisedCompactly(), "phase2-fixture", ReceivedAt);

        await store.SaveAsync(original.Batch, original.SourceResources);
        await store.SaveAsync(reserialised.Batch, reserialised.SourceResources);

        ImportBatch reloadedOriginal = (await store.FindBatchAsync(original.Batch.Id))!;
        ImportBatch reloadedReserialised = (await store.FindBatchAsync(reserialised.Batch.Id))!;

        Assert.NotEqual(reloadedOriginal.ContentHash, reloadedReserialised.ContentHash);
        JsonEquivalence.AssertEquivalent(
            System.Text.Encoding.UTF8.GetString(reloadedOriginal.RawPayload.Span),
            System.Text.Encoding.UTF8.GetString(reloadedReserialised.RawPayload.Span));
    }

    [Fact]
    public async Task Importing_identical_content_twice_creates_two_batches_rather_than_reusing_one()
    {
        await using OncoBridgeDbContext context = await postgres.CreateMigratedContextAsync();
        ImportBatchStore store = new(context);
        FhirBundleIngestor ingestor = new();

        IngestedBundle first = ingestor.Ingest(
            SyntheticFixtures.MinimalBundleBytes, "phase2-fixture", ReceivedAt);
        IngestedBundle second = ingestor.Ingest(
            SyntheticFixtures.MinimalBundleBytes, "phase2-fixture", ReceivedAt);

        await store.SaveAsync(first.Batch, first.SourceResources);
        await store.SaveAsync(second.Batch, second.SourceResources);

        Assert.Equal(2, await store.CountBatchesAsync());
        Assert.Equal(
            (await store.FindBatchAsync(first.Batch.Id))!.ContentHash,
            (await store.FindBatchAsync(second.Batch.Id))!.ContentHash);
    }

    [Fact]
    public async Task A_source_resource_cannot_reference_a_batch_that_does_not_exist()
    {
        await using OncoBridgeDbContext context = await postgres.CreateMigratedContextAsync();

        context.SourceResources.Add(
            new SourceResource(SourceResourceId.New(), ImportBatchId.New(), entryIndex: 0));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task An_entry_with_no_determinable_resource_type_is_still_persisted()
    {
        await using OncoBridgeDbContext context = await postgres.CreateMigratedContextAsync();
        ImportBatchStore store = new(context);

        byte[] payload = SyntheticFixtures.Utf8(
            """
            {"resourceType":"Bundle","type":"collection","entry":[
              {"resource":{"resourceType":"NotARealResourceType","id":"bad-1"}}]}
            """);

        IngestedBundle ingested = new FhirBundleIngestor().Ingest(payload, "phase2-fixture", ReceivedAt);
        await store.SaveAsync(ingested.Batch, ingested.SourceResources);

        SourceResource reloaded =
            (await store.GetSourceResourcesAsync(ingested.Batch.Id)).Single();

        Assert.Equal("NotARealResourceType", reloaded.ResourceType);
        Assert.NotNull(reloaded.ResourceJson);
    }
}
