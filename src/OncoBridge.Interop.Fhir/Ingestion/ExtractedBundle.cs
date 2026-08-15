namespace OncoBridge.Interop.Fhir.Ingestion;

public sealed record ExtractedEntry
{
    public required int EntryIndex { get; init; }

    public required ReadOnlyMemory<byte> RawResourceJson { get; init; }

    public string? FullUrl { get; init; }

    public string? ResourceType { get; init; }

    public string? SourceLogicalId { get; init; }

    public string? InterpretationError { get; init; }

    public bool HasResource => !RawResourceJson.IsEmpty;

    public bool IsInterpretable => InterpretationError is null;
}

public sealed record ExtractedBundle
{
    public required string? BundleType { get; init; }

    public required IReadOnlyList<ExtractedEntry> Entries { get; init; }
}
