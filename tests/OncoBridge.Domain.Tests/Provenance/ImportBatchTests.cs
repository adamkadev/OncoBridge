using System.Text;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Domain.Tests.Provenance;

/// <summary>
/// The byte-preserving audit record, and the guarantee that its digest covers exactly those bytes.
/// </summary>
/// <remarks>
/// These tests encode the Phase 0 correction: the audit representation is the raw bytes, and a
/// digest computed over a parsed or re-serialised form would not be equivalent.
/// </remarks>
public sealed class ImportBatchTests
{
    private static readonly byte[] AnyPayload = Encoding.UTF8.GetBytes(
        """{"resourceType":"Bundle","type":"transaction","entry":[]}""");

    private static ImportBatch CreateBatch(byte[] payload) => ImportBatch.Create(
        ImportBatchId.New(),
        sourceSystemLabel: "synthetic-fixture",
        receivedAt: new DateTimeOffset(2026, 8, 15, 9, 0, 0, TimeSpan.Zero),
        rawPayload: payload);

    [Fact]
    public void The_payload_is_retained_byte_for_byte()
    {
        ImportBatch batch = CreateBatch(AnyPayload);

        Assert.Equal(AnyPayload, batch.RawPayload.ToArray());
    }

    [Fact]
    public void The_digest_covers_exactly_the_retained_bytes()
    {
        ImportBatch batch = CreateBatch(AnyPayload);

        Assert.Equal(ContentHash.ComputeSha256(AnyPayload), batch.ContentHash);
        Assert.True(batch.VerifyPayloadIntegrity());
    }

    /// <summary>
    /// The point of the correction, stated as a test: a semantically identical payload whose bytes
    /// differ — reordered keys and different whitespace — must produce a different digest. A store
    /// that returned this instead of the original would be returning a different document as far as
    /// the audit record is concerned.
    /// </summary>
    [Fact]
    public void A_semantically_equivalent_but_differently_encoded_payload_has_a_different_digest()
    {
        byte[] reordered = Encoding.UTF8.GetBytes(
            """{ "type": "transaction", "resourceType": "Bundle", "entry": [] }""");

        Assert.NotEqual(
            CreateBatch(AnyPayload).ContentHash,
            CreateBatch(reordered).ContentHash);
    }

    /// <summary>An audit record a caller can mutate afterwards is not an audit record.</summary>
    [Fact]
    public void Mutating_the_caller_s_buffer_afterwards_does_not_alter_the_batch()
    {
        byte[] mutable = (byte[])AnyPayload.Clone();
        ImportBatch batch = CreateBatch(mutable);

        mutable[0] = (byte)'X';

        Assert.Equal(AnyPayload, batch.RawPayload.ToArray());
        Assert.True(batch.VerifyPayloadIntegrity());
    }

    [Fact]
    public void An_empty_payload_is_still_hashed_rather_than_treated_as_absent()
    {
        ImportBatch batch = CreateBatch([]);

        Assert.True(batch.VerifyPayloadIntegrity());
        Assert.Equal(ContentHash.ComputeSha256([]), batch.ContentHash);
    }

    [Fact]
    public void A_batch_starts_in_the_received_state()
    {
        ImportBatch batch = CreateBatch(AnyPayload);

        Assert.Equal(ImportBatchStatus.Received, batch.Status);
        Assert.Null(batch.NormalizerVersion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_source_system_label_is_rejected(string label) =>
        Assert.Throws<ArgumentException>(() => ImportBatch.Create(
            ImportBatchId.New(), label, DateTimeOffset.UnixEpoch, AnyPayload));

    [Fact]
    public void A_negative_entry_count_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ImportBatch.Create(
            ImportBatchId.New(), "synthetic-fixture", DateTimeOffset.UnixEpoch, AnyPayload,
            entryCount: -1));
}

/// <summary>Digest computation and parsing.</summary>
public sealed class ContentHashTests
{
    [Fact]
    public void The_digest_of_the_empty_input_is_the_documented_sha256_value() =>
        Assert.Equal(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ContentHash.ComputeSha256([]).Value);

    [Fact]
    public void The_same_bytes_always_produce_the_same_digest()
    {
        byte[] content = Encoding.UTF8.GetBytes("OncoBridge");

        Assert.Equal(ContentHash.ComputeSha256(content), ContentHash.ComputeSha256(content));
    }

    [Fact]
    public void A_single_changed_byte_changes_the_digest() =>
        Assert.NotEqual(
            ContentHash.ComputeSha256(Encoding.UTF8.GetBytes("OncoBridge")),
            ContentHash.ComputeSha256(Encoding.UTF8.GetBytes("OncoBridgf")));

    [Fact]
    public void A_computed_digest_round_trips_through_parsing()
    {
        ContentHash computed = ContentHash.ComputeSha256(Encoding.UTF8.GetBytes("OncoBridge"));

        Assert.Equal(computed, ContentHash.Parse(computed.Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855")] // uppercase
    [InlineData("z3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")] // non-hex
    public void A_malformed_digest_is_rejected(string value) =>
        Assert.Throws<ArgumentException>(() => ContentHash.Parse(value));

    [Fact]
    public void The_algorithm_is_recorded_so_stored_digests_stay_interpretable() =>
        Assert.Equal("SHA-256", ContentHash.Algorithm);
}
