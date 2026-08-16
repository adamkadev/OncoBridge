using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;

namespace OncoBridge.Interop.Fhir.Quality;

internal static class SourceQualityFindings
{
    internal static Finding UnparseableEntry(SourceResource source) => Finding.Create(
        V1CheckIds.UnparseableEntry,
        FindingCategory.Structural,
        FindingSeverity.Error,
        "The bundle entry could not be parsed as a known FHIR R4 resource.",
        FindingTarget.ForSourceResource(source.Id),
        QualityCitations.FhirR4JsonRepresentation,
        expected: "a deserializable FHIR R4 resource",
        actual: DescribeUnparseable(source));

    internal static Finding MissingPrimaryCancerConditionCategory(
        SourceResource source, string statedCategories) => Finding.Create(
        V1CheckIds.PrimaryCancerConditionCategory,
        FindingCategory.Conformance,
        FindingSeverity.Error,
        "The primary cancer condition does not state the mandatory problem-list-item or "
            + "health-concern category.",
        FindingTarget.ForSourceResource(source.Id),
        QualityCitations.McodePrimaryCancerConditionDefinitions,
        expected: $"a Condition.category coding of {CodeSystems.ConditionCategory}|problem-list-item "
            + $"or {CodeSystems.UsCoreConditionCategory}|health-concern",
        actual: statedCategories);

    internal static Finding MissingStageGroupMethod(SourceResource source) => Finding.Create(
        V1CheckIds.StageGroupMethod,
        FindingCategory.Conformance,
        FindingSeverity.Error,
        "The TNM stage group does not state a staging method.",
        FindingTarget.ForSourceResource(source.Id),
        QualityCitations.McodeTnmStageGroup,
        expected: "Observation.method to be present, which mCODE STU4 states as cardinality 1..1",
        actual: "Observation.method is absent");

    internal static Finding UnresolvedReference(
        SourceResource source, string fieldPath, string reference) => Finding.Create(
        V1CheckIds.UnresolvedReference,
        FindingCategory.ReferentialIntegrity,
        FindingSeverity.Error,
        $"The reference at {fieldPath} does not resolve within this import batch. OncoBridge V1 "
            + "resolves references against the resources received in the same batch only, and makes "
            + "no external resolution attempt.",
        FindingTarget.ForSourceResource(source.Id),
        QualityCitations.FhirR4BundleReferenceResolution,
        expected: "a reference resolving to exactly one resource in the same import batch",
        actual: $"{fieldPath} = '{reference}'");

    internal static Finding StageGroupSubjectDisagreement(
        SourceResource source,
        string memberReference,
        string groupSubject,
        string memberSubject) => Finding.Create(
        V1CheckIds.StageGroupSubjectDisagreement,
        FindingCategory.ReferentialIntegrity,
        FindingSeverity.Error,
        "A TNM stage group member observation names a different subject from its stage group.",
        FindingTarget.ForSourceResource(source.Id),
        QualityCitations.McodeTnmStageGroup,
        expected: "every T/N/M member observation to name the same patient as its stage group",
        actual: $"stage group subject '{groupSubject}'; member '{memberReference}' "
            + $"subject '{memberSubject}'");

    private static string DescribeUnparseable(SourceResource source)
    {
        if (string.IsNullOrWhiteSpace(source.ResourceJson))
        {
            return "the entry carries no resource content";
        }

        return string.IsNullOrWhiteSpace(source.ResourceType)
            ? "an entry stating no resourceType"
            : $"an entry stating resourceType '{source.ResourceType}'";
    }
}
