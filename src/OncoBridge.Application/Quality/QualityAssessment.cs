using OncoBridge.Domain.Quality;

namespace OncoBridge.Application.Quality;

public sealed record QualityAssessment
{
    public required IReadOnlyList<Finding> Findings { get; init; }

    public required IReadOnlyList<CoverageNote> CoverageNotes { get; init; }
}
