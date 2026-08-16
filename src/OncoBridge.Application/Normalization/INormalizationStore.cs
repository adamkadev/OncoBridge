using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Application.Normalization;

public interface INormalizationStore
{
    Task<NormalizationSource?> LoadAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default);

    Task ReplaceDerivedAsync(
        ImportBatchId batchId,
        NormalizationResult result,
        string normalizerVersion,
        DateTimeOffset normalizedAt,
        CancellationToken cancellationToken = default);
}
