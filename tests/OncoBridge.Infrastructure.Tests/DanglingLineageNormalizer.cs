using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Infrastructure.Tests;

internal sealed class DanglingLineageNormalizer : ICanonicalNormalizer
{
    private readonly FhirNormalizer _normalizer = new();

    public string Version => _normalizer.Version;

    public NormalizationResult Normalize(IReadOnlyList<SourceResource> sourceResources)
    {
        NormalizationResult result = _normalizer.Normalize(sourceResources);

        return result with
        {
            Lineage =
            [
                .. result.Lineage,
                Lineage.ForEntity("Patient", Guid.NewGuid(), SourceResourceId.New(), "Dangling", "1.0.0"),
            ],
        };
    }
}
