using OncoBridge.Application.Imports;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;

namespace OncoBridge.Interop.Fhir.Tests;

public sealed class FhirBundleIngestorTests
{
    private static readonly DateTimeOffset ReceivedAt = new(2026, 8, 15, 9, 0, 0, TimeSpan.Zero);

    private readonly FhirBundleIngestor _ingestor = new();

    private IngestedPayload IngestMinimalBundle() =>
        _ingestor.Ingest(SyntheticFixtures.MinimalBundleBytes, "phase2-fixture", ReceivedAt, "bundle-minimal.json");

    [Fact]
    public void The_batch_retains_the_exact_payload_and_its_digest()
    {
        IngestedPayload ingested = IngestMinimalBundle();

        Assert.Equal(SyntheticFixtures.MinimalBundleBytes, ingested.Batch.RawPayload.ToArray());
        Assert.Equal(
            ContentHash.ComputeSha256(SyntheticFixtures.MinimalBundleBytes),
            ingested.Batch.ContentHash);
        Assert.True(ingested.Batch.VerifyPayloadIntegrity());
    }

    [Fact]
    public void The_batch_records_the_bundle_type_and_entry_count()
    {
        IngestedPayload ingested = IngestMinimalBundle();

        Assert.Equal("collection", ingested.Batch.BundleType);
        Assert.Equal(4, ingested.Batch.EntryCount);
        Assert.Equal(4, ingested.SourceResources.Count);
    }

    [Fact]
    public void Every_source_resource_belongs_to_the_batch_and_keeps_its_entry_index()
    {
        IngestedPayload ingested = IngestMinimalBundle();

        Assert.All(ingested.SourceResources, resource => Assert.Equal(ingested.Batch.Id, resource.BatchId));
        Assert.Equal([0, 1, 2, 3], ingested.SourceResources.Select(resource => resource.EntryIndex));
    }

    [Fact]
    public void Each_source_resource_digest_is_reproducible_from_the_payload_alone()
    {
        ReadOnlySpan<byte> payload = SyntheticFixtures.MinimalBundleBytes;
        IngestedPayload ingested = IngestMinimalBundle();

        foreach (SourceResource resource in ingested.SourceResources)
        {
            Assert.NotNull(resource.ResourceJson);
            byte[] fragment = System.Text.Encoding.UTF8.GetBytes(resource.ResourceJson!);

            Assert.True(payload.IndexOf(fragment) >= 0);
            Assert.Equal(ContentHash.ComputeSha256(fragment), resource.ContentHash);
        }
    }

    [Fact]
    public void Ingesting_the_same_payload_twice_produces_two_distinct_batches()
    {
        IngestedPayload first = IngestMinimalBundle();
        IngestedPayload second = IngestMinimalBundle();

        Assert.NotEqual(first.Batch.Id, second.Batch.Id);
        Assert.Equal(first.Batch.ContentHash, second.Batch.ContentHash);
    }

    [Fact]
    public void An_entry_without_a_resource_yields_a_source_resource_with_no_content()
    {
        byte[] payload = SyntheticFixtures.Utf8(
            """
            {"resourceType":"Bundle","type":"transaction","entry":[
              {"fullUrl":"urn:uuid:no-resource","request":{"method":"DELETE","url":"Patient/1"}}]}
            """);

        SourceResource resource = _ingestor
            .Ingest(payload, "phase2-fixture", ReceivedAt)
            .SourceResources
            .Single();

        Assert.Null(resource.ResourceType);
        Assert.Null(resource.ContentHash);
        Assert.Null(resource.ResourceJson);
        Assert.Equal("urn:uuid:no-resource", resource.FullUrl);
    }
}
