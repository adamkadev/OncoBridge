namespace OncoBridge.Interop.Fhir.Ingestion;

public sealed record BundleIngestionOptions
{
    public static BundleIngestionOptions Default { get; } = new();

    public int MaxPayloadBytes { get; init; } = 16 * 1024 * 1024;

    public int MaxEntryCount { get; init; } = 10_000;
}
