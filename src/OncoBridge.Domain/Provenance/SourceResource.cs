using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Domain.Provenance;

public sealed class SourceResource
{
    private SourceResource()
    {
    }

    public SourceResource(
        SourceResourceId id,
        ImportBatchId batchId,
        int entryIndex,
        string? resourceType = null,
        ContentHash? contentHash = null,
        string? resourceJson = null,
        string? sourceLogicalId = null,
        string? fullUrl = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(entryIndex);

        Id = id;
        BatchId = batchId;
        EntryIndex = entryIndex;
        ResourceType = resourceType;
        ContentHash = contentHash;
        ResourceJson = resourceJson;
        SourceLogicalId = sourceLogicalId;
        FullUrl = fullUrl;
    }

    public SourceResourceId Id { get; }

    public ImportBatchId BatchId { get; }

    public int EntryIndex { get; }

    public string? ResourceType { get; }

    public ContentHash? ContentHash { get; }

    public string? ResourceJson { get; }

    public string? SourceLogicalId { get; }

    public string? FullUrl { get; }
}
