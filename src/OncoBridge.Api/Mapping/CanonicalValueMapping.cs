using OncoBridge.Api.Contracts;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Api.Mapping;

internal static class CanonicalValueMapping
{
    internal static CodedConceptResponse ToResponse(CodedConcept concept) => new()
    {
        System = concept.System,
        Code = concept.Code,
        Display = concept.Display,
    };

    internal static CodedConceptResponse? ToResponseOrNull(CodedConcept? concept) =>
        concept is null ? null : ToResponse(concept);

    internal static PartialDateResponse ToResponse(PartialDate date) => new()
    {
        Value = date.ToString(),
        Precision = date.Precision.ToString(),
    };

    internal static PartialDateResponse? ToResponseOrNull(PartialDate? date) =>
        date is null ? null : ToResponse(date);

    internal static PartialPeriodResponse ToResponse(PartialPeriod period) => new()
    {
        Start = ToResponseOrNull(period.Start),
        End = ToResponseOrNull(period.End),
    };

    internal static TemporalOccurrenceResponse ToResponse(TemporalOccurrence occurrence) => new()
    {
        Kind = occurrence.Kind.ToString(),
        Date = ToResponseOrNull(occurrence.Date),
        Period = occurrence.Period is { } period ? ToResponse(period) : null,
    };

    internal static TemporalOccurrenceResponse? ToResponseOrNull(TemporalOccurrence? occurrence) =>
        occurrence is null ? null : ToResponse(occurrence);
}
