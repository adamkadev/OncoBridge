using OncoBridge.Application.Quality;
using OncoBridge.Domain.Quality;
using OncoBridge.Interop.Fhir.Tests.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Quality;

public sealed class CoverageNoteTests
{
    private static SourceQualityAssessment AssessProfiledConditionWith(params string[] members) =>
        QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            NormalizationFixtures.ConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                [
                    NormalizationFixtures.Profile(
                        NormalizationFixtures.PrimaryCancerConditionProfile),
                    NormalizationFixtures.BreastCancerCode,
                    QualityFixtures.ProblemListItemCategory,
                    .. members,
                ]));

    [Fact]
    public void A_valid_resource_type_outside_V1_coverage_yields_a_note_and_no_finding()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.AssessBundle(SyntheticFixtures.Phase4Bundle("bundle-structural-malformed"));

        CoverageNote note = Assert.Single(
            assessment.CoverageNotes, candidate => candidate.Subject == "MedicationRequest");

        Assert.Contains("was not examined", note.Reason);
        Assert.DoesNotContain(assessment.Findings, finding => finding.Target.Id == note.Target!.Id);
    }

    [Fact]
    public void A_coverage_note_cannot_be_counted_among_findings()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.AssessBundle(SyntheticFixtures.Phase4Bundle("bundle-structural-malformed"));

        Assert.NotEmpty(assessment.CoverageNotes);
        Assert.IsType<CoverageNote>(assessment.CoverageNotes[0]);
        Assert.False(typeof(Finding).IsAssignableFrom(typeof(CoverageNote)));
        Assert.All(assessment.Findings, finding => Assert.IsType<Finding>(finding));
    }

    [Fact]
    public void An_identifier_only_covered_reference_is_recorded_as_uncovered_not_as_a_defect()
    {
        SourceQualityAssessment assessment = AssessProfiledConditionWith(
            """ "subject":{"identifier":{"system":"urn:oncobridge:synthetic:mrn","value":"SYN-0001"}} """);

        CoverageNote note = Assert.Single(
            assessment.CoverageNotes, candidate => candidate.Subject == "Condition.subject");

        Assert.Contains("identifier matching", note.Reason);
        Assert.Empty(QualityFixtures.FindingsFor(assessment, V1CheckIds.UnresolvedReference));
    }

    [Fact]
    public void An_onset_stated_in_a_form_V1_does_not_read_is_recorded_as_uncovered()
    {
        SourceQualityAssessment assessment = AssessProfiledConditionWith(
            NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
            """ "onsetString":"about three years ago" """);

        CoverageNote note = Assert.Single(
            assessment.CoverageNotes,
            candidate => candidate.Subject == "Condition.onset[x] stated as string");

        Assert.Equal("V1 reads an occurrence stated as a dateTime or a Period only.", note.Reason);
        Assert.Empty(assessment.Findings);
    }

    [Fact]
    public void A_performed_stated_in_a_form_V1_does_not_read_is_recorded_as_uncovered()
    {
        const string Ucum = "http://unitsofmeasure.org";
        const string PerformedAge =
            $$""" "performedAge":{"value":51,"unit":"years","system":"{{Ucum}}","code":"a"} """;

        SourceQualityAssessment assessment = QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            NormalizationFixtures.SurgicalProcedureEntry(
                NormalizationFixtures.ProcedureFullUrl,
                "procedure-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.LumpectomyCode,
                PerformedAge));

        Assert.Single(
            assessment.CoverageNotes,
            note => note.Subject == "Procedure.performed[x] stated as Age");
        Assert.Empty(assessment.Findings);
    }

    [Fact]
    public void A_supported_occurrence_form_produces_no_coverage_note() =>
        Assert.Empty(AssessProfiledConditionWith(
            NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
            """ "onsetDateTime":"2019-03" """).CoverageNotes);

    [Fact]
    public void A_clean_covered_source_graph_produces_no_notes_and_no_findings()
    {
        SourceQualityAssessment assessment =
            QualityFixtures.AssessBundle(SyntheticFixtures.Phase4Bundle("bundle-clean-source"));

        Assert.Empty(assessment.Findings);
        Assert.Empty(assessment.CoverageNotes);
    }
}
