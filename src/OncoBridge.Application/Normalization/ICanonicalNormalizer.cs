using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Normalization;

public interface ICanonicalNormalizer
{
    string Version { get; }

    NormalizationResult Normalize(IReadOnlyList<SourceResource> sourceResources);
}
