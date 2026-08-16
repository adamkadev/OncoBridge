using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Domain.Provenance;

public enum ImportBatchStatus
{
    Received,
    Normalized,
    Failed,
}

public sealed class ImportBatch
{
    private ImportBatch() => SourceSystemLabel = string.Empty;

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
        RawPayload = rawPayload;
        ContentHash = contentHash;
        FileName = fileName;
        BundleType = bundleType;
        EntryCount = entryCount;
        Status = status;
        NormalizerVersion = normalizerVersion;
    }

    public ImportBatchId Id { get; }

    public string SourceSystemLabel { get; }

    public DateTimeOffset ReceivedAt { get; }

    public ReadOnlyMemory<byte> RawPayload { get; private set; }

    public ContentHash ContentHash { get; }

    public string? FileName { get; }

    public string? BundleType { get; }

    public int EntryCount { get; }

    public ImportBatchStatus Status { get; private set; }

    public string? NormalizerVersion { get; private set; }

    public DateTimeOffset? NormalizedAt { get; private set; }

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

    public void MarkNormalized(string normalizerVersion, DateTimeOffset normalizedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizerVersion);

        if (normalizedAt < ReceivedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalizedAt),
                normalizedAt,
                $"Normalization cannot complete before the batch was received at {ReceivedAt:O}.");
        }

        Status = ImportBatchStatus.Normalized;
        NormalizerVersion = normalizerVersion;
        NormalizedAt = normalizedAt;
    }

    public bool VerifyPayloadIntegrity() =>
        ContentHash.ComputeSha256(RawPayload.Span) == ContentHash;
}
