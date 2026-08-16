using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

internal static class StagingFixtures
{
    internal const string PatientLogicalId = "patient-001";

    internal const string ConditionLogicalId = "condition-001";

    internal const string StageGroupLogicalId = "stage-group-001";

    internal const string PrimaryTumourLogicalId = "stage-t-001";

    internal const string RegionalNodesLogicalId = "stage-n-001";

    internal const string DistantMetastasesLogicalId = "stage-m-001";

    internal const string StageGroupValue = "IIA";

    internal const string PrimaryTumourValue = "T2";

    internal const string RegionalNodesValue = "N1";

    internal const string DistantMetastasesValue = "M0";

    internal static string PatientEntry() =>
        NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, PatientLogicalId);

    internal static string ConditionEntry() =>
        NormalizationFixtures.PrimaryCancerConditionEntry(
            NormalizationFixtures.ConditionFullUrl,
            ConditionLogicalId,
            NormalizationFixtures.PatientFullUrl,
            NormalizationFixtures.BreastCancerCode);

    internal static NormalizationResult NormalizeStaging(params string[] entries) =>
        NormalizationFixtures.NormalizeEntries([PatientEntry(), ConditionEntry(), .. entries]);

    internal static string ObservationEntry(
        string fullUrl, string logicalId, string loincCode, params string[] members) =>
        NormalizationFixtures.ObservationEntry(
            fullUrl, logicalId, [NormalizationFixtures.LoincCode(loincCode), .. members]);

    internal static string StageGroupEntry(params string[] members) =>
        ObservationEntry(
            NormalizationFixtures.StageGroupFullUrl,
            StageGroupLogicalId,
            NormalizationFixtures.ClinicalStageGroupCode,
            members);

    internal static string LinkedStageGroupEntry(params string[] members) =>
        StageGroupEntry(
        [
            NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
            NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
            .. members,
        ]);

    internal static string PrimaryTumourEntry(string value = PrimaryTumourValue) =>
        CategoryEntry(
            NormalizationFixtures.PrimaryTumourFullUrl,
            PrimaryTumourLogicalId,
            NormalizationFixtures.ClinicalPrimaryTumourCode,
            value);

    internal static string RegionalNodesEntry(string value = RegionalNodesValue) =>
        CategoryEntry(
            NormalizationFixtures.RegionalNodesFullUrl,
            RegionalNodesLogicalId,
            NormalizationFixtures.ClinicalRegionalNodesCode,
            value);

    internal static string DistantMetastasesEntry(string value = DistantMetastasesValue) =>
        CategoryEntry(
            NormalizationFixtures.DistantMetastasesFullUrl,
            DistantMetastasesLogicalId,
            NormalizationFixtures.ClinicalDistantMetastasesCode,
            value);

    internal static string CategoryEntry(
        string fullUrl, string logicalId, string loincCode, string value) =>
        ObservationEntry(
            fullUrl,
            logicalId,
            loincCode,
            NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
            NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
            NormalizationFixtures.StagingValue(value));
}
