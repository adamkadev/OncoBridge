using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Domain.Provenance;

/// <summary>
/// One resource as it was received within an import batch.
/// </summary>
/// <remarks>
/// <para>
/// This is the addressable unit that structural, conformance and referential-integrity findings
/// attach to (ADR-0004). Those findings are statements about the input, so they remain true no
/// matter how normalisation later changes.
/// </para>
/// <para>
/// <b>No parsed JSON field exists in Phase 1, deliberately.</b> A queryable representation of the
/// resource belongs to P2 persistence, and when it arrives it must be understood as a derived
/// convenience for semantic access — not as the audit record. The byte-preserving record is
/// <see cref="ImportBatch.RawPayload"/> together with its digest. Adding a JSON field now would
/// invite exactly the confusion the Phase 0 correction removed.
/// </para>
/// <para>
/// <see cref="ContentHash"/> here covers the exact byte range this entry occupied within the
/// received payload, on the same terms.
/// </para>
/// </remarks>
public sealed class SourceResource
{
    /// <summary>Creates a source resource record.</summary>
    /// <param name="id">This source resource's identity.</param>
    /// <param name="batchId">The batch it arrived in.</param>
    /// <param name="resourceType">The resource type as declared by the source.</param>
    /// <param name="contentHash">SHA-256 over the exact bytes of this entry.</param>
    /// <param name="entryIndex">Zero-based position within the received payload.</param>
    /// <param name="sourceLogicalId">The logical id the source assigned, if any.</param>
    /// <param name="fullUrl">The entry's full URL, used for intra-payload reference resolution.</param>
    /// <exception cref="ArgumentException"><paramref name="resourceType"/> is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="entryIndex"/> is negative.</exception>
    public SourceResource(
        SourceResourceId id,
        ImportBatchId batchId,
        string resourceType,
        ContentHash contentHash,
        int entryIndex,
        string? sourceLogicalId = null,
        string? fullUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentOutOfRangeException.ThrowIfNegative(entryIndex);

        Id = id;
        BatchId = batchId;
        ResourceType = resourceType;
        ContentHash = contentHash;
        EntryIndex = entryIndex;
        SourceLogicalId = sourceLogicalId;
        FullUrl = fullUrl;
    }

    /// <summary>This source resource's identity.</summary>
    public SourceResourceId Id { get; }

    /// <summary>The batch it arrived in.</summary>
    public ImportBatchId BatchId { get; }

    /// <summary>The resource type as declared by the source, carried through unchanged.</summary>
    public string ResourceType { get; }

    /// <summary>SHA-256 over the exact bytes of this entry.</summary>
    public ContentHash ContentHash { get; }

    /// <summary>Zero-based position within the received payload.</summary>
    public int EntryIndex { get; }

    /// <summary>The logical id the source assigned, if any.</summary>
    public string? SourceLogicalId { get; }

    /// <summary>The entry's full URL, used for intra-payload reference resolution.</summary>
    public string? FullUrl { get; }
}
