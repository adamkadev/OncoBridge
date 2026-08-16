using OncoBridge.Application.Quality;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Interop.Fhir.Quality;
using OncoBridge.Interop.Fhir.Tests.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Quality;

internal static class QualityFixtures
{
    internal const string ConditionCategorySystem =
        "http://terminology.hl7.org/CodeSystem/condition-category";

    internal const string UsCoreConditionCategorySystem =
        "http://hl7.org/fhir/us/core/CodeSystem/condition-category";

    internal const string ProblemListItemCategory =
        $$""" "category":[{"coding":[{"system":"{{ConditionCategorySystem}}","code":"problem-list-item"}]}] """;

    internal const string HealthConcernCategory =
        $$""" "category":[{"coding":[{"system":"{{UsCoreConditionCategorySystem}}","code":"health-concern"}]}] """;

    internal const string EncounterDiagnosisCategory =
        $$""" "category":[{"coding":[{"system":"{{ConditionCategorySystem}}","code":"encounter-diagnosis"}]}] """;

    internal const string StagingMethod =
        """ "method":{"coding":[{"system":"http://snomed.info/sct","code":"254292007"}]} """;

    internal static SourceQualityAssessment Assess(IReadOnlyList<SourceResource> sourceResources) =>
        new FhirSourceQualityEvaluator().Assess(sourceResources);

    internal static SourceQualityAssessment AssessEntries(params string[] entries) =>
        Assess(NormalizationFixtures.IngestEntries(entries).SourceResources);

    internal static SourceQualityAssessment AssessBundle(byte[] payload) =>
        Assess(NormalizationFixtures.Ingest(payload).SourceResources);

    internal static Finding[] FindingsFor(SourceQualityAssessment assessment, CheckId checkId) =>
        [.. assessment.Findings.Where(finding => finding.CheckId == checkId)];

    internal static string[] CheckIdsOf(SourceQualityAssessment assessment) =>
        [.. assessment.Findings.Select(finding => finding.CheckId.Value)];

    internal static string PatientEntry(string fullUrl = NormalizationFixtures.PatientFullUrl) =>
        NormalizationFixtures.PatientEntry(fullUrl, "patient-001");

    internal static string PrimaryCancerCondition(
        string subjectReference = NormalizationFixtures.PatientFullUrl,
        params string[] members) =>
        NormalizationFixtures.PrimaryCancerConditionEntry(
            NormalizationFixtures.ConditionFullUrl,
            "condition-001",
            subjectReference,
            [NormalizationFixtures.BreastCancerCode, ProblemListItemCategory, .. members]);

    internal static string StageGroup(params string[] members) =>
        StagingFixtures.LinkedStageGroupEntry([StagingMethod, .. members]);

    internal static string PrimaryTumour() => StagingFixtures.PrimaryTumourEntry();
}
