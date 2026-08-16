using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class CancerSurgicalProcedureRecognitionTests
{
    private static NormalizationResult NormalizeProcedureDeclaring(params string[] meta) =>
        NormalizationFixtures.NormalizeEntries(
            ProcedureFixtures.PatientEntry(),
            NormalizationFixtures.ProcedureEntry(
                NormalizationFixtures.ProcedureFullUrl,
                ProcedureFixtures.ProcedureLogicalId,
                [
                    .. meta,
                    NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                    NormalizationFixtures.LumpectomyCode,
                ]));

    [Fact]
    public void A_profiled_cancer_related_surgical_procedure_becomes_a_cancer_surgical_procedure()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeSurgicalProcedureBundle();

        Assert.Single(result.CancerSurgicalProcedures);
    }

    [Fact]
    public void A_profile_stated_with_a_version_suffix_is_still_recognised()
    {
        NormalizationResult result = NormalizeProcedureDeclaring(
            NormalizationFixtures.Profile(
                NormalizationFixtures.CancerRelatedSurgicalProcedureProfile + "|4.0.0"));

        Assert.Single(result.CancerSurgicalProcedures);
    }

    [Fact]
    public void A_procedure_declaring_no_profile_at_all_is_not_normalized()
    {
        Assert.Empty(NormalizeProcedureDeclaring().CancerSurgicalProcedures);
    }

    [Theory]
    [InlineData("http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-cancer-related-radiation-procedure")]
    [InlineData("http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-cancer-related-surgical-procedure-x")]
    public void A_procedure_declaring_another_profile_is_not_normalized(string canonical)
    {
        NormalizationResult result =
            NormalizeProcedureDeclaring(NormalizationFixtures.Profile(canonical));

        Assert.Empty(result.CancerSurgicalProcedures);
    }

    [Fact]
    public void A_procedure_is_never_recognised_from_its_code_alone()
    {
        NormalizationResult result = NormalizeProcedureDeclaring();

        Assert.Empty(result.CancerSurgicalProcedures);
        Assert.Empty(result.Patients);
    }
}
