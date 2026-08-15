using OncoBridge.Interop.Fhir.Ingestion;

namespace OncoBridge.Interop.Fhir.Tests;

public sealed class FhirBundleExtractorTests
{
    private readonly FhirBundleExtractor _extractor = new();

    [Fact]
    public void Extracts_every_entry_preserving_source_order()
    {
        ExtractedBundle bundle = _extractor.Extract(SyntheticFixtures.MinimalBundleBytes);

        Assert.Equal("collection", bundle.BundleType);
        Assert.Equal(
            ["Patient", "Condition", "Observation", "Procedure"],
            bundle.Entries.Select(entry => entry.ResourceType));
        Assert.Equal([0, 1, 2, 3], bundle.Entries.Select(entry => entry.EntryIndex));
    }

    [Fact]
    public void Extracts_logical_ids_and_full_urls()
    {
        ExtractedBundle bundle = _extractor.Extract(SyntheticFixtures.MinimalBundleBytes);

        Assert.Equal(
            ["patient-001", "condition-001", "observation-001", "procedure-001"],
            bundle.Entries.Select(entry => entry.SourceLogicalId));
        Assert.All(bundle.Entries, entry => Assert.StartsWith("urn:uuid:", entry.FullUrl));
    }

    [Fact]
    public void Each_extracted_resource_is_a_verbatim_contiguous_slice_of_the_payload()
    {
        ReadOnlySpan<byte> payload = SyntheticFixtures.MinimalBundleBytes;
        ExtractedBundle bundle = _extractor.Extract(SyntheticFixtures.MinimalBundleBytes);

        foreach (ExtractedEntry entry in bundle.Entries)
        {
            Assert.True(
                payload.IndexOf(entry.RawResourceJson.Span) >= 0,
                $"Entry {entry.EntryIndex} was not found verbatim inside the payload.");
        }
    }

    [Fact]
    public void The_condition_slice_preserves_the_non_canonical_key_order_of_the_source()
    {
        ExtractedBundle bundle = _extractor.Extract(SyntheticFixtures.MinimalBundleBytes);

        string condition = System.Text.Encoding.UTF8.GetString(bundle.Entries[1].RawResourceJson.Span);

        Assert.True(
            condition.IndexOf("\"id\"", StringComparison.Ordinal)
                < condition.IndexOf("\"resourceType\"", StringComparison.Ordinal),
            "The fixture states id before resourceType; a re-serialised slice would reorder them.");
    }

    [Fact]
    public void A_malformed_entry_does_not_prevent_the_remaining_entries_from_being_extracted()
    {
        byte[] payload = SyntheticFixtures.Utf8(
            """
            {
              "resourceType": "Bundle",
              "type": "collection",
              "entry": [
                { "resource": { "resourceType": "Patient", "id": "good-1" } },
                { "resource": { "resourceType": "NotARealResourceType", "id": "bad-1" } },
                { "resource": { "resourceType": "Patient", "id": "good-2" } }
              ]
            }
            """);

        ExtractedBundle bundle = _extractor.Extract(payload);

        Assert.Equal(3, bundle.Entries.Count);
        Assert.True(bundle.Entries[0].IsInterpretable);
        Assert.False(bundle.Entries[1].IsInterpretable);
        Assert.True(bundle.Entries[2].IsInterpretable);
        Assert.Equal("good-2", bundle.Entries[2].SourceLogicalId);
    }

    [Fact]
    public void A_malformed_entry_still_retains_its_raw_bytes_for_later_inspection()
    {
        byte[] payload = SyntheticFixtures.Utf8(
            """
            {"resourceType":"Bundle","type":"collection","entry":[
              {"resource":{"resourceType":"NotARealResourceType","id":"bad-1"}}]}
            """);

        ExtractedEntry entry = _extractor.Extract(payload).Entries.Single();

        Assert.False(entry.IsInterpretable);
        Assert.True(entry.HasResource);
        Assert.True(((ReadOnlySpan<byte>)payload).IndexOf(entry.RawResourceJson.Span) >= 0);
    }

    [Fact]
    public void An_entry_carrying_no_resource_is_still_extracted()
    {
        byte[] payload = SyntheticFixtures.Utf8(
            """
            {"resourceType":"Bundle","type":"transaction","entry":[
              {"fullUrl":"urn:uuid:no-resource","request":{"method":"DELETE","url":"Patient/1"}}]}
            """);

        ExtractedEntry entry = _extractor.Extract(payload).Entries.Single();

        Assert.False(entry.HasResource);
        Assert.Null(entry.ResourceType);
        Assert.Equal("urn:uuid:no-resource", entry.FullUrl);
    }

    [Fact]
    public void A_bundle_without_entries_extracts_to_an_empty_list()
    {
        ExtractedBundle bundle = _extractor.Extract(
            SyntheticFixtures.Utf8("""{"resourceType":"Bundle","type":"collection"}"""));

        Assert.Empty(bundle.Entries);
    }

    [Theory]
    [InlineData("""{"resourceType":"Patient","id":"p1"}""", "not a FHIR Bundle")]
    [InlineData("""["not","an","object"]""", "not a JSON object")]
    [InlineData("""{"resourceType":"Bundle","entry":{"not":"an array"}}""", "not an array")]
    public void Input_that_is_not_an_acceptable_bundle_is_rejected(string json, string expectedReason)
    {
        BundleIngestionException exception = Assert.Throws<BundleIngestionException>(
            () => _extractor.Extract(SyntheticFixtures.Utf8(json)));

        Assert.Contains(expectedReason, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Invalid_json_is_rejected_as_an_ingestion_failure_rather_than_a_json_exception()
    {
        BundleIngestionException exception = Assert.Throws<BundleIngestionException>(
            () => _extractor.Extract(SyntheticFixtures.Utf8("{ this is not json ")));

        Assert.Contains("not valid JSON", exception.Message, StringComparison.Ordinal);
        Assert.IsType<System.Text.Json.JsonException>(exception.InnerException, exactMatch: false);
    }

    [Fact]
    public void An_empty_payload_is_rejected() =>
        Assert.Throws<BundleIngestionException>(() => _extractor.Extract(ReadOnlyMemory<byte>.Empty));

    [Fact]
    public void A_payload_over_the_configured_limit_is_rejected()
    {
        FhirBundleExtractor extractor = new(new BundleIngestionOptions { MaxPayloadBytes = 32 });

        Assert.Throws<BundleIngestionException>(
            () => extractor.Extract(SyntheticFixtures.MinimalBundleBytes));
    }

    [Fact]
    public void A_bundle_with_more_entries_than_the_configured_limit_is_rejected()
    {
        FhirBundleExtractor extractor = new(new BundleIngestionOptions { MaxEntryCount = 2 });

        Assert.Throws<BundleIngestionException>(
            () => extractor.Extract(SyntheticFixtures.MinimalBundleBytes));
    }

    [Fact]
    public void Rejection_messages_do_not_echo_payload_content()
    {
        BundleIngestionException exception = Assert.Throws<BundleIngestionException>(
            () => _extractor.Extract(SyntheticFixtures.Utf8(
                """{"resourceType":"Patient","id":"SECRET-VALUE-0001"}""")));

        Assert.DoesNotContain("SECRET-VALUE-0001", exception.Message, StringComparison.Ordinal);
    }
}
