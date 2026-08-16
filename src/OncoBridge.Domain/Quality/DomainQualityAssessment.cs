namespace OncoBridge.Domain.Quality;

public sealed record DomainQualityAssessment
{
    public required IReadOnlyList<Finding> Findings { get; init; }

    public required IReadOnlyList<CoverageNote> CoverageNotes { get; init; }
}
