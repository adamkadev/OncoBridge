using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Interop.Fhir.Quality;

internal static class SourceQualityCoverage
{
    internal static CoverageNote ResourceTypeOutsideCoverage(
        SourceResource source, string resourceType) => CoverageNote.Create(
        resourceType,
        "V1 source quality examines Patient, Condition, Observation and Procedure only; this "
            + "resource type was not examined.",
        FindingTarget.ForSourceResource(source.Id));

    internal static CoverageNote IdentifierOnlyReference(SourceResource source, string fieldPath) =>
        CoverageNote.Create(
            fieldPath,
            "The reference states an identifier and no literal reference; V1 resolves literal "
                + "references only and attempted no identifier matching.",
            FindingTarget.ForSourceResource(source.Id));

    internal static CoverageNote UnreadOccurrenceForm(
        SourceResource source, string fieldPath, string statedType) => CoverageNote.Create(
        $"{fieldPath} stated as {statedType}",
        "V1 reads an occurrence stated as a dateTime or a Period only.",
        FindingTarget.ForSourceResource(source.Id));
}
