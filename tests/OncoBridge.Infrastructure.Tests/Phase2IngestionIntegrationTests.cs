using OncoBridge.Application.Imports;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OncoBridge.Domain.Provenance;
using OncoBridge.Infrastructure.Persistence;
using OncoBridge.Interop.Fhir.Ingestion;

namespace OncoBridge.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class Phase2IngestionIntegrationTests(PostgreSqlFixture postgres)
{
    private static readonly DateTimeOffset ReceivedAt = new(2026, 8, 15, 9, 30, 0, TimeSpan.FromHours(2));

    private static IngestedPayload IngestFixture() =>
        new FhirBundleIngestor().Ingest(
            SyntheticFixtures.MinimalBundleBytes, "phase2-fixture", ReceivedAt, "bundle-minimal.json");

    private async Task<(OncoBridgeDbContext Context, IngestedPayload Ingested)> PersistFixtureAsync()
    {
        OncoBridgeDbContext context = await postgres.CreateMigratedContextAsync();
        IngestedPayload ingested = IngestFixture();

        await new ImportBatchStore(context).SaveAsync(ingested.Batch, ingested.SourceResources);

        return (context, ingested);
    }

    [Fact]
    public async Task Exactly_one_import_batch_is_persisted()
    {
        (OncoBridgeDbContext context, _) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        Assert.Equal(1, await new ImportBatchStore(context).CountBatchesAsync());
    }

    [Fact]
    public async Task The_reloaded_raw_payload_is_byte_for_byte_identical_to_the_fixture()
    {
        (OncoBridgeDbContext context, IngestedPayload ingested) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        ImportBatch? reloaded = await new ImportBatchStore(context).FindBatchAsync(ingested.Batch.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(SyntheticFixtures.MinimalBundleBytes, reloaded!.RawPayload.ToArray());
    }

    [Fact]
    public async Task The_reloaded_digest_equals_the_digest_of_the_fixture_bytes()
    {
        (OncoBridgeDbContext context, IngestedPayload ingested) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        ImportBatch reloaded = (await new ImportBatchStore(context).FindBatchAsync(ingested.Batch.Id))!;

        Assert.Equal(ContentHash.ComputeSha256(SyntheticFixtures.MinimalBundleBytes), reloaded.ContentHash);
        Assert.True(reloaded.VerifyPayloadIntegrity());
    }

    [Fact]
    public async Task The_reloaded_batch_records_the_bundle_type_entry_count_and_receipt_instant()
    {
        (OncoBridgeDbContext context, IngestedPayload ingested) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        ImportBatch reloaded = (await new ImportBatchStore(context).FindBatchAsync(ingested.Batch.Id))!;

        Assert.Equal("collection", reloaded.BundleType);
        Assert.Equal(4, reloaded.EntryCount);
        Assert.Equal("bundle-minimal.json", reloaded.FileName);
        Assert.Equal(ImportBatchStatus.Received, reloaded.Status);
        Assert.Equal(ReceivedAt.ToUniversalTime(), reloaded.ReceivedAt);
    }

    [Fact]
    public async Task One_source_resource_is_persisted_per_bundle_entry_in_source_order()
    {
        (OncoBridgeDbContext context, IngestedPayload ingested) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        IReadOnlyList<SourceResource> reloaded =
            await new ImportBatchStore(context).GetSourceResourcesAsync(ingested.Batch.Id);

        Assert.Equal(4, reloaded.Count);
        Assert.Equal([0, 1, 2, 3], reloaded.Select(resource => resource.EntryIndex));
        Assert.Equal(
            ["Patient", "Condition", "Observation", "Procedure"],
            reloaded.Select(resource => resource.ResourceType));
        Assert.Equal(
            ["patient-001", "condition-001", "observation-001", "procedure-001"],
            reloaded.Select(resource => resource.SourceLogicalId));
        Assert.All(reloaded, resource => Assert.StartsWith("urn:uuid:", resource.FullUrl));
    }

    [Fact]
    public async Task Reloaded_resource_json_is_semantically_equivalent_to_the_input()
    {
        (OncoBridgeDbContext context, IngestedPayload ingested) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        IReadOnlyList<SourceResource> reloaded =
            await new ImportBatchStore(context).GetSourceResourcesAsync(ingested.Batch.Id);

        foreach (SourceResource expected in ingested.SourceResources)
        {
            SourceResource actual = reloaded.Single(r => r.EntryIndex == expected.EntryIndex);
            JsonEquivalence.AssertEquivalent(expected.ResourceJson!, actual.ResourceJson!);
        }
    }

    [Fact]
    public async Task Reloaded_resource_json_is_equivalent_but_not_byte_identical()
    {
        (OncoBridgeDbContext context, IngestedPayload ingested) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        SourceResource condition =
            (await new ImportBatchStore(context).GetSourceResourcesAsync(ingested.Batch.Id))
            .Single(resource => resource.ResourceType == "Condition");

        string original = ingested.SourceResources.Single(r => r.EntryIndex == 1).ResourceJson!;

        Assert.True(JsonEquivalence.AreEquivalent(original, condition.ResourceJson!));
        Assert.NotEqual(original, condition.ResourceJson);
        Assert.NotEqual(
            ContentHash.ComputeSha256(Encoding.UTF8.GetBytes(original)),
            ContentHash.ComputeSha256(Encoding.UTF8.GetBytes(condition.ResourceJson!)));
    }

    [Fact]
    public async Task The_resource_digest_still_matches_the_original_payload_slice_after_reload()
    {
        (OncoBridgeDbContext context, IngestedPayload ingested) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        ImportBatch batch = (await new ImportBatchStore(context).FindBatchAsync(ingested.Batch.Id))!;
        IReadOnlyList<SourceResource> reloaded =
            await new ImportBatchStore(context).GetSourceResourcesAsync(ingested.Batch.Id);

        ReadOnlySpan<byte> payload = batch.RawPayload.Span;

        foreach (SourceResource resource in reloaded)
        {
            ExtractedEntry entry = new FhirBundleExtractor()
                .Extract(batch.RawPayload)
                .Entries
                .Single(e => e.EntryIndex == resource.EntryIndex);

            Assert.True(payload.IndexOf(entry.RawResourceJson.Span) >= 0);
            Assert.Equal(ContentHash.ComputeSha256(entry.RawResourceJson.Span), resource.ContentHash);
        }
    }

    [Fact]
    public async Task The_payload_column_is_bytea_and_the_resource_json_column_is_jsonb()
    {
        (OncoBridgeDbContext context, _) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        Assert.Equal("bytea", await ColumnTypeAsync(context, "import_batch", "raw_payload"));
        Assert.Equal("jsonb", await ColumnTypeAsync(context, "source_resource", "resource_json"));
    }

    [Fact]
    public async Task The_schema_was_created_by_the_committed_migration()
    {
        (OncoBridgeDbContext context, _) = await PersistFixtureAsync();
        await using OncoBridgeDbContext _context = context;

        IEnumerable<string> applied = await context.Database.GetAppliedMigrationsAsync();

        Assert.Contains(applied, migration => migration.EndsWith("InitialProvenanceSchema", StringComparison.Ordinal));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    private static async Task<string> ColumnTypeAsync(
        OncoBridgeDbContext context, string table, string column)
    {
        await using Npgsql.NpgsqlCommand query = new(
            "SELECT udt_name FROM information_schema.columns "
                + "WHERE table_name = @table AND column_name = @column",
            (Npgsql.NpgsqlConnection)context.Database.GetDbConnection());

        query.Parameters.AddWithValue("table", table);
        query.Parameters.AddWithValue("column", column);

        await context.Database.OpenConnectionAsync();

        return (string)(await query.ExecuteScalarAsync())!;
    }
}
