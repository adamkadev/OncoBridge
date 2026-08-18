using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Application.Reading;

public sealed class GetImport(IOncoBridgeReadStore readStore)
{
    public Task<ImportDetails?> ExecuteAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default) =>
        readStore.GetImportAsync(batchId, cancellationToken);
}
