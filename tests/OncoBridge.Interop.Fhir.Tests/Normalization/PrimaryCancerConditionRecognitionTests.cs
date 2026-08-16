using OncoBridge.Application.Normalization;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class PrimaryCancerConditionRecognitionTests
{
    private static NormalizationResult NormalizeConditionWith(params string[] members) =>
        NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.ConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                [NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl), .. members]));

    [Fact]
    public void A_condition_declaring_no_profile_is_not_treated_as_a_primary_cancer_diagnosis()
    {
        NormalizationResult result = NormalizeConditionWith(NormalizationFixtures.BreastCancerCode);

        Assert.Empty(result.PrimaryCancerDiagnoses);
        Assert.Empty(result.Patients);
    }

    [Fact]
    public void A_cancer_code_alone_does_not_make_a_condition_a_primary_cancer_diagnosis()
    {
        NormalizationResult result = NormalizeConditionWith(
            """ "meta":{"profile":["http://hl7.org/fhir/StructureDefinition/Condition"]} """,
            NormalizationFixtures.BreastCancerCode);

        Assert.Empty(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_non_cancer_condition_is_not_normalized_merely_for_being_a_condition()
    {
        NormalizationResult result = NormalizeConditionWith(
            """
            "code":{"coding":[{"system":"http://snomed.info/sct","code":"38341003",
                               "display":"Hypertensive disorder"}]}
            """);

        Assert.Empty(result.PrimaryCancerDiagnoses);
        Assert.Empty(result.Patients);
    }

    [Fact]
    public void The_mcode_profile_is_recognized_with_a_version_suffix()
    {
        NormalizationResult result = NormalizeConditionWith(
            $$""" "meta":{"profile":["{{NormalizationFixtures.PrimaryCancerConditionProfile}}|4.0.0"]} """,
            NormalizationFixtures.BreastCancerCode);

        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void The_mcode_profile_is_recognized_alongside_other_declared_profiles()
    {
        NormalizationResult result = NormalizeConditionWith(
            $$"""
             "meta":{"profile":["http://hl7.org/fhir/StructureDefinition/Condition",
                                "{{NormalizationFixtures.PrimaryCancerConditionProfile}}"]}
            """,
            NormalizationFixtures.BreastCancerCode);

        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_resource_that_is_not_a_condition_is_never_normalized_as_a_diagnosis()
    {
        NormalizationResult result = NormalizationFixtures.Normalize(
            NormalizationFixtures.Ingest(SyntheticFixtures.MinimalBundleBytes).SourceResources);

        Assert.Empty(result.PrimaryCancerDiagnoses);
        Assert.Empty(result.Patients);
        Assert.Empty(result.Lineage);
    }
}
