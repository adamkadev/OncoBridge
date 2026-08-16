using OncoBridge.Application.Normalization;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class NormalizationResilienceTests
{
    private static NormalizationResult NormalizeAlongsideAHealthyCondition(params string[] members) =>
        NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                "urn:uuid:condition-defective",
                "condition-defective",
                NormalizationFixtures.PatientFullUrl,
                members),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

    [Fact]
    public void A_condition_with_no_usable_coding_produces_no_diagnosis_and_stops_nothing_else()
    {
        NormalizationResult result = NormalizeAlongsideAHealthyCondition(
            """ "code":{"text":"Breast cancer, stated only as free text"} """);

        Assert.Single(result.PrimaryCancerDiagnoses);
        Assert.Single(result.Patients);
    }

    [Fact]
    public void A_condition_whose_codings_all_lack_a_system_or_a_code_produces_no_diagnosis()
    {
        NormalizationResult result = NormalizeAlongsideAHealthyCondition(
            """ "code":{"coding":[{"display":"Nothing usable"},{"system":"http://snomed.info/sct"}]} """);

        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void An_unsupported_onset_representation_does_not_fabricate_a_temporal_value()
    {
        NormalizationResult result = NormalizeAlongsideAHealthyCondition(
            NormalizationFixtures.BreastCancerCode,
            """ "onsetAge":{"value":51,"unit":"years","system":"http://unitsofmeasure.org","code":"a"} """);

        Assert.Equal(2, result.PrimaryCancerDiagnoses.Count);
        Assert.All(result.PrimaryCancerDiagnoses, diagnosis => Assert.Null(diagnosis.Onset));
    }

    [Fact]
    public void An_onset_string_is_not_parsed_into_a_temporal_value()
    {
        NormalizationResult result = NormalizeAlongsideAHealthyCondition(
            NormalizationFixtures.BreastCancerCode,
            """ "onsetString":"about three years ago" """);

        Assert.Equal(2, result.PrimaryCancerDiagnoses.Count);
        Assert.All(result.PrimaryCancerDiagnoses, diagnosis => Assert.Null(diagnosis.Onset));
    }

    [Fact]
    public void An_uninterpretable_source_resource_stops_nothing_else()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.Entry(
                "urn:uuid:condition-broken",
                $$"""
                {"resourceType":"Condition","id":"broken",
                 "meta":{"profile":["{{NormalizationFixtures.PrimaryCancerConditionProfile}}"]},
                 "subject":{"reference":"{{NormalizationFixtures.PatientFullUrl}}"},
                 "onsetDateTime":{"not":"a dateTime"},
                 {{NormalizationFixtures.BreastCancerCode}}}
                """),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void An_empty_source_list_normalizes_to_nothing()
    {
        NormalizationResult result = NormalizationFixtures.Normalize([]);

        Assert.Empty(result.Patients);
        Assert.Empty(result.PrimaryCancerDiagnoses);
        Assert.Empty(result.Lineage);
    }
}
