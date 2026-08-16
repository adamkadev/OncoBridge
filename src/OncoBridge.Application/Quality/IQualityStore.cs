using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Application.Quality;

public interface IQualityStore
{
    Task<QualityAssessmentSource?> LoadAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default);

    Task ReplaceFindingsAsync(
        ImportBatchId batchId,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default);
}
