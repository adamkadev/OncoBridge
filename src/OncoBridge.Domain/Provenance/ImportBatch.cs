using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Domain.Provenance;

/// <summary>How far an import batch has progressed.</summary>
public enum ImportBatchStatus
{
    /// <summary>Payload received and hashed; nothing normalised yet.</summary>
    Received,

    /// <summary>Normalisation completed for this batch.</summary>
    Normalized,

    /// <summary>Processing failed. The payload is retained regardless.</summary>
    Failed,
}

/// <summary>
/// One ingestion run: the unit of provenance, retry and reporting, and the holder of the exact
/// bytes that were received.
/// </summary>
/// <remarks>
/// <para><b><see cref="RawPayload"/> is the audit representation, and it is the bytes.</b></para>
/// <para>
/// This is the point corrected after Phase 0 review. An earlier draft treated a queryable JSON
/// column as the byte-preserving record; that was wrong. Storing JSON in a parsed, queryable form
/// normalises it — key order, whitespace, number formatting and string escapes may all change — so
/// such a column preserves <i>meaning</i>, not <i>bytes</i>. It cannot support an audit claim about
/// what was received, and it cannot reproduce the original digest.
/// </para>
/// <para>The resulting split, which persistence in P2 must honour:</para>
/// <list type="bullet">
///   <item><description>
///     <see cref="RawPayload"/> — the exact uploaded bytes, byte-for-byte. Intended for a
///     byte-preserving column (PostgreSQL <c>bytea</c>). Never re-encoded, never reformatted.
///   </description></item>
///   <item><description>
///     <see cref="ContentHash"/> — SHA-256 over precisely those bytes, computed here at
///     construction so the two can never drift apart.
///   </description></item>
///   <item><description>
///     A parsed, queryable JSON representation of individual resources may be added to
///     <see cref="SourceResource"/> in P2 for semantic access. It is a <i>derived convenience</i>
///     and is explicitly <b>not</b> the audit representation.
///   </description></item>
/// </list>
/// <para>
/// The P2 gate therefore has two separate obligations: (a) exact byte round-trip of
/// <see cref="RawPayload"/>, and (b) semantic persistence of parsed resources. Proving (b) does
/// not prove (a).
/// </para>
/// </remarks>
public sealed class ImportBatch
{
    private readonly byte[] _rawPayload;

    private ImportBatch(
        ImportBatchId id,
        string sourceSystemLabel,
        DateTimeOffset receivedAt,
        byte[] rawPayload,
        ContentHash contentHash,
        string? fileName,
        string? bundleType,
        int entryCount,
        ImportBatchStatus status,
        string? normalizerVersion)
    {
        Id = id;
        SourceSystemLabel = sourceSystemLabel;
        ReceivedAt = receivedAt;
        _rawPayload = rawPayload;
        ContentHash = contentHash;
        FileName = fileName;
        BundleType = bundleType;
        EntryCount = entryCount;
        Status = status;
        NormalizerVersion = normalizerVersion;
    }

    /// <summary>This batch's identity.</summary>
    public ImportBatchId Id { get; }

    /// <summary>A label for where the payload came from, e.g. the generator that produced it.</summary>
    public string SourceSystemLabel { get; }

    /// <summary>When OncoBridge received the payload. System time.</summary>
    public DateTimeOffset ReceivedAt { get; }

    /// <summary>
    /// The exact bytes received, unmodified. Exposed as <see cref="ReadOnlyMemory{T}"/> so callers
    /// cannot mutate the audit record.
    /// </summary>
    public ReadOnlyMemory<byte> RawPayload => _rawPayload;

    /// <summary>SHA-256 over exactly the bytes in <see cref="RawPayload"/>.</summary>
    public ContentHash ContentHash { get; }

    /// <summary>The supplied file name, if any.</summary>
    public string? FileName { get; }

    /// <summary>The bundle type as declared by the payload, if known.</summary>
    public string? BundleType { get; }

    /// <summary>How many entries the payload declared.</summary>
    public int EntryCount { get; }

    /// <summary>How far this batch has progressed.</summary>
    public ImportBatchStatus Status { get; }

    /// <summary>The normaliser version that processed this batch, once one has.</summary>
    public string? NormalizerVersion { get; }

    /// <summary>
    /// Creates a batch from the exact bytes received, computing <see cref="ContentHash"/> over
    /// them.
    /// </summary>
    /// <remarks>
    /// The hash is computed here rather than accepted as a parameter, so a caller cannot supply a
    /// digest that does not match the payload. The bytes are copied defensively: an audit record
    /// that a caller can mutate afterwards is not an audit record.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="sourceSystemLabel"/> is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="entryCount"/> is negative.</exception>
    public static ImportBatch Create(
        ImportBatchId id,
        string sourceSystemLabel,
        DateTimeOffset receivedAt,
        ReadOnlySpan<byte> rawPayload,
        string? fileName = null,
        string? bundleType = null,
        int entryCount = 0,
        ImportBatchStatus status = ImportBatchStatus.Received,
        string? normalizerVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSystemLabel);
        ArgumentOutOfRangeException.ThrowIfNegative(entryCount);

        byte[] copy = rawPayload.ToArray();

        return new ImportBatch(
            id,
            sourceSystemLabel,
            receivedAt,
            copy,
            ContentHash.ComputeSha256(copy),
            fileName,
            bundleType,
            entryCount,
            status,
            normalizerVersion);
    }

    /// <summary>
    /// Recomputes the digest over the retained bytes and confirms it still matches
    /// <see cref="ContentHash"/>.
    /// </summary>
    /// <remarks>
    /// This is the domain-level expression of the byte round-trip obligation. Once persistence
    /// exists in P2, the same check run after a load proves the store returned the bytes it was
    /// given rather than a re-encoded equivalent.
    /// </remarks>
    public bool VerifyPayloadIntegrity() =>
        ContentHash.ComputeSha256(_rawPayload) == ContentHash;
}
