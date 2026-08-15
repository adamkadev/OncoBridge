namespace OncoBridge.Interop.Fhir.Ingestion;

public sealed record BundleIngestionOptions
{
    public const int DefaultMaxPayloadBytes = 16 * 1024 * 1024;

    public const int DefaultMaxEntryCount = 10_000;

    public static BundleIngestionOptions Default { get; } = new();

    public int MaxPayloadBytes { get; init; } = DefaultMaxPayloadBytes;

    public int MaxEntryCount { get; init; } = DefaultMaxEntryCount;
}
