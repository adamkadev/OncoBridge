using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Terminology;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class CancerSurgicalProcedureMappingTests
{
    [Fact]
    public void The_procedure_code_preserves_system_code_and_display_exactly()
    {
        CancerSurgicalProcedure procedure =
            Assert.Single(NormalizationFixtures.NormalizeSurgicalProcedureBundle().CancerSurgicalProcedures);

        Assert.Equal(
            new CodedConcept("http://snomed.info/sct", "392021009", "Lumpectomy of breast (procedure)"),
            procedure.Code);
    }

    [Fact]
    public void The_first_coding_carrying_both_a_system_and_a_code_is_selected()
    {
        CancerSurgicalProcedure procedure = Assert.Single(
            ProcedureFixtures.NormalizeProcedureStating(
                """
                "code":{"coding":[
                    {"display":"No system and no code"},
                    {"system":"http://snomed.info/sct"},
                    {"code":"392021009"},
                    {"system":"http://snomed.info/sct","code":"392021009","display":"Chosen"},
                    {"system":"http://www.ama-assn.org/go/cpt","code":"19301","display":"Later"}]}
                """)
            .CancerSurgicalProcedures);

        Assert.Equal(
            new CodedConcept("http://snomed.info/sct", "392021009", "Chosen"), procedure.Code);
    }

    [Fact]
    public void Coding_selection_repeats_the_same_choice_on_every_run()
    {
        const string Code =
            """
            "code":{"coding":[
                {"system":"http://snomed.info/sct","code":"392021009","display":"First"},
                {"system":"http://www.ama-assn.org/go/cpt","code":"19301","display":"Second"}]}
            """;

        Assert.Equal(
            Assert.Single(ProcedureFixtures.NormalizeProcedureStating(Code).CancerSurgicalProcedures).Code,
            Assert.Single(ProcedureFixtures.NormalizeProcedureStating(Code).CancerSurgicalProcedures).Code);
    }

    [Fact]
    public void A_procedure_with_no_usable_coding_produces_nothing_and_stops_no_sibling_procedure()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            ProcedureFixtures.PatientEntry(),
            NormalizationFixtures.SurgicalProcedureEntry(
                "urn:uuid:procedure-defective",
                "procedure-defective",
                NormalizationFixtures.PatientFullUrl,
                """ "code":{"text":"Lumpectomy, stated only as free text"} """),
            ProcedureFixtures.SurgicalProcedureEntry(NormalizationFixtures.LumpectomyCode));

        CancerSurgicalProcedure procedure = Assert.Single(result.CancerSurgicalProcedures);

        Assert.Equal(ProcedureFixtures.LumpectomySnomedCode, procedure.Code.Code);
    }

    [Fact]
    public void The_first_usable_body_site_coding_across_the_stated_concepts_is_selected()
    {
        CancerSurgicalProcedure procedure = ProcedureFixtures.NormalizeProcedureWith(
            """
            "bodySite":[
                {"coding":[{"display":"No system and no code"}]},
                {"coding":[
                    {"system":"http://snomed.info/sct","code":"76752008","display":"Chosen"},
                    {"system":"http://snomed.info/sct","code":"80248007","display":"Later"}]}]
            """);

        Assert.Equal(
            new CodedConcept("http://snomed.info/sct", "76752008", "Chosen"), procedure.BodySite);
    }

    [Fact]
    public void An_absent_body_site_is_not_invented()
    {
        Assert.Null(ProcedureFixtures.NormalizeProcedureWith().BodySite);
    }
}
