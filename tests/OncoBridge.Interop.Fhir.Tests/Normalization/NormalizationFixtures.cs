using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

internal static class NormalizationFixtures
{
    internal const string PrimaryCancerConditionProfile =
        "http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-primary-cancer-condition";

    internal const string PatientFullUrl = "urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa";

    internal const string ConditionFullUrl = "urn:uuid:bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb";

    internal const string StageGroupFullUrl = "urn:uuid:cccccccc-3333-4333-8333-cccccccccccc";

    internal const string PrimaryTumourFullUrl = "urn:uuid:dddddddd-4444-4444-8444-dddddddddddd";

    internal const string RegionalNodesFullUrl = "urn:uuid:eeeeeeee-5555-4555-8555-eeeeeeeeeeee";

    internal const string DistantMetastasesFullUrl = "urn:uuid:ffffffff-6666-4666-8666-ffffffffffff";

    internal const string ClinicalStageGroupCode = "21908-9";

    internal const string PathologicalStageGroupCode = "21902-2";

    internal const string OtherStageGroupCode = "21914-7";

    internal const string ClinicalPrimaryTumourCode = "21905-5";

    internal const string PathologicalPrimaryTumourCode = "21899-0";

    internal const string OtherPrimaryTumourCode = "21911-3";

    internal const string ClinicalRegionalNodesCode = "21906-3";

    internal const string PathologicalRegionalNodesCode = "21900-6";

    internal const string OtherRegionalNodesCode = "21912-1";

    internal const string ClinicalDistantMetastasesCode = "21907-1";

    internal const string PathologicalDistantMetastasesCode = "21901-4";

    internal const string OtherDistantMetastasesCode = "21913-9";

    internal const string StagingCodeSystem = "http://cancerstaging.org";

    internal const string BreastCancerCode =
        """ "code":{"coding":[{"system":"http://snomed.info/sct","code":"254837009"}]} """;

    private const string Loinc = "http://loinc.org";

    private const string Status = """ "status":"final" """;

    private const string PrimaryCancerProfile =
        $$""" "meta":{"profile":["{{PrimaryCancerConditionProfile}}"]} """;

    private static readonly DateTimeOffset ReceivedAt = new(2026, 8, 16, 9, 0, 0, TimeSpan.Zero);

    internal static IngestedBundle Ingest(byte[] payload) =>
        new FhirBundleIngestor().Ingest(payload, "phase3-fixture", ReceivedAt);

    internal static IngestedBundle IngestEntries(params string[] entries) =>
        Ingest(SyntheticFixtures.Utf8(Bundle(entries)));

    internal static IngestedBundle IngestPrimaryCancerBundle() =>
        Ingest(SyntheticFixtures.PrimaryCancerBundleBytes);

    internal static IngestedBundle IngestTnmStagingBundle() =>
        Ingest(SyntheticFixtures.TnmStagingBundleBytes);

    internal static NormalizationResult Normalize(IReadOnlyList<SourceResource> sourceResources) =>
        new FhirNormalizer().Normalize(sourceResources);

    internal static NormalizationResult NormalizeEntries(params string[] entries) =>
        Normalize(IngestEntries(entries).SourceResources);

    internal static NormalizationResult NormalizePrimaryCancerBundle() =>
        Normalize(IngestPrimaryCancerBundle().SourceResources);

    internal static NormalizationResult NormalizeTnmStagingBundle() =>
        Normalize(IngestTnmStagingBundle().SourceResources);

    internal static string Bundle(params string[] entries) =>
        $$"""{"resourceType":"Bundle","type":"collection","entry":[{{string.Join(",", entries)}}]}""";

    internal static string Entry(string fullUrl, string resource) =>
        $$"""{"fullUrl":"{{fullUrl}}","resource":{{resource}}}""";

    internal static string PatientEntry(string fullUrl, string logicalId, params string[] members) =>
        Entry(fullUrl, JsonObject([Header("Patient", logicalId), .. members]));

    internal static string ConditionEntry(string fullUrl, string logicalId, params string[] members) =>
        Entry(fullUrl, JsonObject([Header("Condition", logicalId), .. members]));

    internal static string PrimaryCancerConditionEntry(
        string fullUrl, string logicalId, string subjectReference, params string[] members) =>
        ConditionEntry(fullUrl, logicalId, [PrimaryCancerProfile, Subject(subjectReference), .. members]);

    internal static string ObservationEntry(string fullUrl, string logicalId, params string[] members) =>
        Entry(fullUrl, JsonObject([Header("Observation", logicalId), Status, .. members]));

    internal static string Subject(string reference) =>
        $$""" "subject":{"reference":"{{reference}}"} """;

    internal static string Focus(params string[] references) => ReferenceArray("focus", references);

    internal static string HasMember(params string[] references) =>
        ReferenceArray("hasMember", references);

    internal static string LoincCode(string code) =>
        $$""" "code":{"coding":[{"system":"{{Loinc}}","code":"{{code}}"}]} """;

    internal static string StagingValue(string code) => StagingValue(StagingCodeSystem, code, null);

    internal static string StagingValue(string system, string code, string? display) =>
        $$""" "valueCodeableConcept":{"coding":[{{Coding(system, code, display)}}]} """;

    internal static string Method(string system, string code, string? display = null) =>
        $$""" "method":{"coding":[{{Coding(system, code, display)}}]} """;

    internal static string EffectiveDateTime(string value) =>
        $$""" "effectiveDateTime":"{{value}}" """;

    private static string Coding(string system, string code, string? display) =>
        display is null
            ? $$"""{"system":"{{system}}","code":"{{code}}"}"""
            : $$"""{"system":"{{system}}","code":"{{code}}","display":"{{display}}"}""";

    private static string ReferenceArray(string element, IEnumerable<string> references) =>
        $""" "{element}":[{string.Join(",", references.Select(Reference))}] """;

    private static string Reference(string reference) =>
        $$"""{"reference":"{{reference}}"}""";

    private static string Header(string resourceType, string logicalId) =>
        $$""" "resourceType":"{{resourceType}}","id":"{{logicalId}}" """;

    private static string JsonObject(IEnumerable<string> members) => $"{{{string.Join(",", members)}}}";
}
