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

    internal const string BreastCancerCode =
        """ "code":{"coding":[{"system":"http://snomed.info/sct","code":"254837009"}]} """;

    private const string PrimaryCancerProfile =
        $$""" "meta":{"profile":["{{PrimaryCancerConditionProfile}}"]} """;

    private static readonly DateTimeOffset ReceivedAt = new(2026, 8, 16, 9, 0, 0, TimeSpan.Zero);

    internal static IngestedBundle Ingest(byte[] payload) =>
        new FhirBundleIngestor().Ingest(payload, "phase3-fixture", ReceivedAt);

    internal static IngestedBundle IngestEntries(params string[] entries) =>
        Ingest(SyntheticFixtures.Utf8(Bundle(entries)));

    internal static IngestedBundle IngestPrimaryCancerBundle() =>
        Ingest(SyntheticFixtures.PrimaryCancerBundleBytes);

    internal static NormalizationResult Normalize(IReadOnlyList<SourceResource> sourceResources) =>
        new FhirNormalizer().Normalize(sourceResources);

    internal static NormalizationResult NormalizeEntries(params string[] entries) =>
        Normalize(IngestEntries(entries).SourceResources);

    internal static NormalizationResult NormalizePrimaryCancerBundle() =>
        Normalize(IngestPrimaryCancerBundle().SourceResources);

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

    internal static string Subject(string reference) =>
        $$""" "subject":{"reference":"{{reference}}"} """;

    private static string Header(string resourceType, string logicalId) =>
        $$""" "resourceType":"{{resourceType}}","id":"{{logicalId}}" """;

    private static string JsonObject(IEnumerable<string> members) => $"{{{string.Join(",", members)}}}";
}
