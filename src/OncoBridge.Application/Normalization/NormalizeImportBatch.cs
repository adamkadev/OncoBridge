using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Application.Normalization;

public sealed class NormalizeImportBatch(
    ICanonicalNormalizer normalizer,
    INormalizationStore store,
    TimeProvider timeProvider)
{
    public async Task<NormalizationResult?> ExecuteAsync(
        ImportBatchId batchId, CancellationToken cancellationToken = default)
    {
        if (await store.LoadAsync(batchId, cancellationToken) is not { } source)
        {
            return null;
        }

        NormalizationResult result = normalizer.Normalize(source.SourceResources);

        await store.ReplaceDerivedAsync(
            batchId,
            result,
            normalizer.Version,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return result;
    }
}
