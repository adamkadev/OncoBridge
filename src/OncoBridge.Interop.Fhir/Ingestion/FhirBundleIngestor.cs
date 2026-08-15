using System.Text;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Interop.Fhir.Ingestion;

public sealed record IngestedBundle(ImportBatch Batch, IReadOnlyList<SourceResource> SourceResources);

public sealed class FhirBundleIngestor
{
    private readonly FhirBundleExtractor _extractor;

    public FhirBundleIngestor(FhirBundleExtractor? extractor = null) =>
        _extractor = extractor ?? new FhirBundleExtractor();

    public IngestedBundle Ingest(
        ReadOnlyMemory<byte> payload,
        string sourceSystemLabel,
        DateTimeOffset receivedAt,
        string? fileName = null)
    {
        ExtractedBundle extracted = _extractor.Extract(payload);
        ImportBatchId batchId = ImportBatchId.New();

        ImportBatch batch = ImportBatch.Create(
            batchId,
            sourceSystemLabel,
            receivedAt,
            payload.Span,
            fileName,
            extracted.BundleType,
            extracted.Entries.Count);

        SourceResource[] sourceResources =
            [.. extracted.Entries.Select(entry => ToSourceResource(entry, batchId))];

        return new IngestedBundle(batch, sourceResources);
    }

    private static SourceResource ToSourceResource(ExtractedEntry entry, ImportBatchId batchId) => new(
        SourceResourceId.New(),
        batchId,
        entry.EntryIndex,
        entry.ResourceType,
        entry.HasResource ? ContentHash.ComputeSha256(entry.RawResourceJson.Span) : null,
        entry.HasResource ? Encoding.UTF8.GetString(entry.RawResourceJson.Span) : null,
        entry.SourceLogicalId,
        entry.FullUrl);
}
