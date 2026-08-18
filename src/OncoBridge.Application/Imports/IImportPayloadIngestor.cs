namespace OncoBridge.Application.Imports;

public interface IImportPayloadIngestor
{
    IngestedPayload Ingest(
        ReadOnlyMemory<byte> payload,
        string sourceSystemLabel,
        DateTimeOffset receivedAt,
        string? fileName = null);
}
