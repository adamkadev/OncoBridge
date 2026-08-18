using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Imports;

public interface IImportBatchWriter
{
    Task SaveAsync(
        ImportBatch batch,
        IReadOnlyList<SourceResource> sourceResources,
        CancellationToken cancellationToken = default);
}
