using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Quality;

public interface ISourceQualityEvaluator
{
    SourceQualityAssessment Assess(IReadOnlyList<SourceResource> sourceResources);
}
