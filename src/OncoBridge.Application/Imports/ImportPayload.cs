using OncoBridge.Application.Normalization;
using OncoBridge.Application.Quality;
using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Application.Imports;

public sealed class ImportPayload(
    IImportPayloadIngestor ingestor,
    IImportBatchWriter writer,
    NormalizeImportBatch normalize,
    AssessImportBatch assess,
    TimeProvider timeProvider)
{
    public async Task<ImportBatchId> ExecuteAsync(
        ReadOnlyMemory<byte> payload,
        string sourceSystemLabel,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        IngestedPayload ingested =
            ingestor.Ingest(payload, sourceSystemLabel, timeProvider.GetUtcNow(), fileName);

        ImportBatchId batchId = ingested.Batch.Id;

        await writer.SaveAsync(ingested.Batch, ingested.SourceResources, cancellationToken);

        if (await normalize.ExecuteAsync(batchId, cancellationToken) is null)
        {
            throw new InvalidOperationException(
                $"Import batch '{batchId}' was just persisted but normalization could not load it.");
        }

        if (await assess.ExecuteAsync(batchId, cancellationToken) is null)
        {
            throw new InvalidOperationException(
                $"Import batch '{batchId}' was just persisted but quality assessment could not load it.");
        }

        return batchId;
    }
}
