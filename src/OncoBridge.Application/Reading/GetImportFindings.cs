using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Application.Reading;

public sealed class GetImportFindings(IOncoBridgeReadStore readStore)
{
    public Task<IReadOnlyList<Finding>?> ExecuteAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default) =>
        readStore.GetFindingsAsync(batchId, cancellationToken);
}
