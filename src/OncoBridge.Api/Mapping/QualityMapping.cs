using OncoBridge.Api.Contracts;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Api.Mapping;

internal static class QualityMapping
{
    internal static FindingResponse ToResponse(Finding finding) => new()
    {
        CheckId = finding.CheckId.Value,
        Category = finding.Category.ToString(),
        Severity = finding.Severity.ToString(),
        Message = finding.Message,
        Target = ToResponse(finding.Target),
        Citation = finding.Citation,
        Expected = finding.Expected,
        Actual = finding.Actual,
    };

    private static FindingTargetResponse ToResponse(FindingTarget target) => new()
    {
        Kind = target.Kind.ToString(),
        Id = target.Id,
        DomainEntityType = target.DomainEntityType,
    };
}
