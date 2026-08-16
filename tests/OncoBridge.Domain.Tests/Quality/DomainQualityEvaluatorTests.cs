using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Quality;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Tests.Quality;

public sealed class DomainQualityEvaluatorTests
{
    private static readonly PatientId Patient = PatientId.New();

    private static readonly CodedConcept AnyCode = new("http://snomed.info/sct", "254837009");

    private static readonly Guid FirstStagingId = new("11111111-0000-4000-8000-000000000001");

    private static PrimaryCancerDiagnosis Diagnosis(
        PrimaryCancerDiagnosisId id, TemporalOccurrence? onset) =>
        new(id, Patient, AnyCode, onset);

    private static CancerStaging Staging(
        PrimaryCancerDiagnosisId diagnosisId, PartialDate? effective, Guid? id = null) =>
        new(id ?? FirstStagingId, Patient, diagnosisId, AnyCode, effective: effective);

    private static DomainQualityAssessment AssessOnePair(
        TemporalOccurrence? onset, PartialDate? effective)
    {
        PrimaryCancerDiagnosisId diagnosisId = new(Guid.NewGuid());

        return new DomainQualityEvaluator().Assess(
            [Diagnosis(diagnosisId, onset)], [Staging(diagnosisId, effective)]);
    }

    private static DomainQualityAssessment AssessDates(string onset, string effective) =>
        AssessOnePair(TemporalOccurrence.FromDate(Date(onset)), Date(effective));

    private static PartialDate Date(string value)
    {
        string[] parts = value.Split('-');

        return parts.Length switch
        {
            1 => PartialDate.FromYear(int.Parse(parts[0])),
            2 => PartialDate.FromYearMonth(int.Parse(parts[0]), int.Parse(parts[1])),
            _ => PartialDate.FromDate(
                int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2])),
        };
    }

    [Fact]
    public void A_staging_definitely_before_its_diagnosis_onset_is_reported()
    {
        Finding finding = Assert.Single(AssessDates(onset: "2019", effective: "2018").Findings);

        Assert.Equal(V1CheckIds.StagingPrecedesDiagnosis, finding.CheckId);
        Assert.Equal(FindingCategory.DomainConsistency, finding.Category);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void A_staging_after_its_diagnosis_onset_is_not_reported() =>
        Assert.Empty(AssessDates(onset: "2019", effective: "2020").Findings);

    [Fact]
    public void A_staging_at_the_same_stated_time_as_onset_is_not_reported()
    {
        DomainQualityAssessment assessment = AssessDates(onset: "2019", effective: "2019");

        Assert.Empty(assessment.Findings);
        Assert.Empty(assessment.CoverageNotes);
    }

    [Fact]
    public void Precision_that_admits_no_definite_ordering_produces_a_note_and_no_finding()
    {
        DomainQualityAssessment assessment = AssessDates(onset: "2019-03", effective: "2019");

        Assert.Empty(assessment.Findings);

        CoverageNote note = Assert.Single(assessment.CoverageNotes);

        Assert.Contains("no definite ordering", note.Reason);
        Assert.Contains("staging effective: 2019; diagnosis onset: 2019-03", note.Reason);
    }

    [Fact]
    public void A_staging_without_an_effective_time_is_not_evaluated()
    {
        DomainQualityAssessment assessment = AssessOnePair(
            TemporalOccurrence.FromDate(Date("2019")), effective: null);

        Assert.Empty(assessment.Findings);
        Assert.Empty(assessment.CoverageNotes);
    }

    [Fact]
    public void A_diagnosis_without_an_onset_is_not_evaluated()
    {
        DomainQualityAssessment assessment = AssessOnePair(onset: null, effective: Date("2019"));

        Assert.Empty(assessment.Findings);
        Assert.Empty(assessment.CoverageNotes);
    }

    [Fact]
    public void An_onset_period_is_compared_against_its_stated_start()
    {
        DomainQualityAssessment assessment = AssessOnePair(
            TemporalOccurrence.FromPeriod(
                PartialPeriod.Between(Date("2019-06"), Date("2019-09"))),
            effective: Date("2018"));

        Assert.Single(assessment.Findings);
    }

    [Fact]
    public void An_onset_period_end_is_never_treated_as_the_onset()
    {
        DomainQualityAssessment assessment = AssessOnePair(
            TemporalOccurrence.FromPeriod(
                PartialPeriod.Between(Date("2019-06"), Date("2021-09"))),
            effective: Date("2020"));

        Assert.Empty(assessment.Findings);
    }

    [Fact]
    public void An_onset_period_with_no_stated_start_produces_a_note_and_no_finding()
    {
        DomainQualityAssessment assessment = AssessOnePair(
            TemporalOccurrence.FromPeriod(PartialPeriod.EndingAt(Date("2019-09"))),
            effective: Date("2018"));

        Assert.Empty(assessment.Findings);

        CoverageNote note = Assert.Single(assessment.CoverageNotes);

        Assert.Contains("no start boundary", note.Reason);
    }

    [Fact]
    public void The_finding_targets_the_staging_entity_it_is_about()
    {
        Finding finding = Assert.Single(AssessDates(onset: "2019", effective: "2018").Findings);

        Assert.Equal(FindingTargetKind.DomainEntity, finding.Target.Kind);
        Assert.Equal(nameof(CancerStaging), finding.Target.DomainEntityType);
        Assert.Equal(FirstStagingId, finding.Target.Id);
    }

    [Fact]
    public void The_finding_cites_the_accepted_temporal_model_rather_than_a_specification()
    {
        Finding finding = Assert.Single(AssessDates(onset: "2019", effective: "2018").Findings);

        Assert.Equal(
            DomainQualityCitations.VariablePrecisionTemporalModel, finding.Citation);
        Assert.Equal("docs/adr/0005-variable-precision-temporal-model.md", finding.Citation);
    }

    [Fact]
    public void The_finding_states_both_representations_deterministically()
    {
        Finding finding =
            Assert.Single(AssessDates(onset: "2019-06", effective: "2018-05").Findings);

        Assert.Equal(
            "The staging effective time is definitely before the onset of the primary cancer "
                + "diagnosis it stages.",
            finding.Message);
        Assert.Equal("staging effective time not definitely before diagnosis onset", finding.Expected);
        Assert.Equal("staging effective: 2018-05; diagnosis onset: 2019-06", finding.Actual);
    }

    [Fact]
    public void Each_staging_is_compared_to_the_cancer_it_names_not_the_patients_other_cancer()
    {
        PrimaryCancerDiagnosisId early = new(new Guid("22222222-0000-4000-8000-000000000001"));
        PrimaryCancerDiagnosisId late = new(new Guid("22222222-0000-4000-8000-000000000002"));

        DomainQualityAssessment assessment = new DomainQualityEvaluator().Assess(
            [
                Diagnosis(early, TemporalOccurrence.FromDate(Date("2015"))),
                Diagnosis(late, TemporalOccurrence.FromDate(Date("2021"))),
            ],
            [
                Staging(early, Date("2016"), new Guid("33333333-0000-4000-8000-000000000001")),
                Staging(late, Date("2016"), new Guid("33333333-0000-4000-8000-000000000002")),
            ]);

        Finding finding = Assert.Single(assessment.Findings);

        Assert.Equal(new Guid("33333333-0000-4000-8000-000000000002"), finding.Target.Id);
        Assert.Equal("staging effective: 2016; diagnosis onset: 2021", finding.Actual);
    }

    [Fact]
    public void A_staging_naming_a_diagnosis_that_was_not_supplied_is_a_programming_error()
    {
        PrimaryCancerDiagnosisId supplied = new(Guid.NewGuid());
        PrimaryCancerDiagnosisId absent = new(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => new DomainQualityEvaluator().Assess(
            [Diagnosis(supplied, TemporalOccurrence.FromDate(Date("2019")))],
            [Staging(absent, Date("2018"))]));
    }

    [Fact]
    public void Evaluating_equivalent_inputs_twice_produces_equivalent_ordered_results()
    {
        PrimaryCancerDiagnosisId diagnosisId = new(new Guid("44444444-0000-4000-8000-000000000001"));

        PrimaryCancerDiagnosis[] diagnoses =
            [Diagnosis(diagnosisId, TemporalOccurrence.FromDate(Date("2019")))];
        CancerStaging[] stagings =
        [
            Staging(diagnosisId, Date("2018"), new Guid("55555555-0000-4000-8000-000000000002")),
            Staging(diagnosisId, Date("2017"), new Guid("55555555-0000-4000-8000-000000000001")),
        ];

        DomainQualityAssessment first = new DomainQualityEvaluator().Assess(diagnoses, stagings);
        DomainQualityAssessment second = new DomainQualityEvaluator().Assess(diagnoses, stagings);

        Assert.Equal(first.Findings, second.Findings);
        Assert.Equal(
            [
                new Guid("55555555-0000-4000-8000-000000000001"),
                new Guid("55555555-0000-4000-8000-000000000002"),
            ],
            first.Findings.Select(finding => finding.Target.Id));
    }
}
