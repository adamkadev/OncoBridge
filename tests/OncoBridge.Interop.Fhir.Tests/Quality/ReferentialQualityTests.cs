using OncoBridge.Application.Quality;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Tests.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Quality;

public sealed class ReferentialQualityTests
{
    private const string SecondPatientFullUrl = "urn:uuid:aaaaaaaa-1111-4111-8111-bbbbbbbbbbbb";

    private static Finding[] UnresolvedFindings(SourceQualityAssessment assessment) =>
        QualityFixtures.FindingsFor(assessment, V1CheckIds.UnresolvedReference);

    private static Finding[] MismatchFindings(SourceQualityAssessment assessment) =>
        QualityFixtures.FindingsFor(assessment, V1CheckIds.StageGroupSubjectDisagreement);

    [Fact]
    public void A_subject_stated_as_a_full_url_resolves() =>
        Assert.Empty(UnresolvedFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            QualityFixtures.PrimaryCancerCondition())));

    [Fact]
    public void A_subject_stated_as_type_and_logical_id_resolves() =>
        Assert.Empty(UnresolvedFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            QualityFixtures.PrimaryCancerCondition("Patient/patient-001"))));

    [Fact]
    public void A_covered_reference_that_names_nothing_in_the_batch_is_reported()
    {
        Finding finding = Assert.Single(UnresolvedFindings(QualityFixtures.AssessBundle(
            SyntheticFixtures.Phase4Bundle("bundle-dangling-reference"))));

        Assert.Equal(FindingCategory.ReferentialIntegrity, finding.Category);
        Assert.Equal(FindingSeverity.Error, finding.Severity);
        Assert.Equal("https://hl7.org/fhir/R4/bundle.html", finding.Citation);
        Assert.Equal(
            "Condition.subject = 'urn:uuid:0badbeef-0000-4000-8000-000000000000'", finding.Actual);
    }

    [Fact]
    public void The_unresolved_reference_message_states_that_V1_never_resolves_outside_the_batch()
    {
        Finding finding = Assert.Single(UnresolvedFindings(QualityFixtures.AssessBundle(
            SyntheticFixtures.Phase4Bundle("bundle-dangling-reference"))));

        Assert.Contains("does not resolve within this import batch", finding.Message);
        Assert.Contains("makes no external resolution attempt", finding.Message);
    }

    [Fact]
    public void A_reference_never_resolves_to_a_resource_from_another_batch()
    {
        IngestedBundle patientBatch =
            NormalizationFixtures.IngestEntries(QualityFixtures.PatientEntry());
        IngestedBundle conditionBatch =
            NormalizationFixtures.IngestEntries(QualityFixtures.PrimaryCancerCondition());

        Assert.NotEqual(patientBatch.Batch.Id, conditionBatch.Batch.Id);

        SourceResource[] bothBatches =
            [.. patientBatch.SourceResources, .. conditionBatch.SourceResources];

        Assert.Single(UnresolvedFindings(QualityFixtures.Assess(bothBatches)));
    }

    [Fact]
    public void An_ambiguous_reference_counts_as_unresolved() =>
        Assert.Single(UnresolvedFindings(QualityFixtures.AssessEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-002"),
            QualityFixtures.PrimaryCancerCondition())));

    [Fact]
    public void A_reference_to_a_resource_contained_in_the_same_resource_resolves() =>
        Assert.Empty(UnresolvedFindings(QualityFixtures.AssessEntries(
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                "#contained-patient",
                NormalizationFixtures.BreastCancerCode,
                QualityFixtures.ProblemListItemCategory,
                """ "contained":[{"resourceType":"Patient","id":"contained-patient"}] """))));

    [Fact]
    public void A_contained_reference_naming_nothing_contained_is_reported() =>
        Assert.Single(UnresolvedFindings(QualityFixtures.AssessEntries(
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                "#absent-patient",
                NormalizationFixtures.BreastCancerCode,
                QualityFixtures.ProblemListItemCategory,
                """ "contained":[{"resourceType":"Patient","id":"contained-patient"}] """))));

    [Fact]
    public void A_procedure_reason_reference_is_covered_and_a_dangling_one_is_reported()
    {
        Finding finding = Assert.Single(UnresolvedFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            NormalizationFixtures.SurgicalProcedureEntry(
                NormalizationFixtures.ProcedureFullUrl,
                "procedure-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.LumpectomyCode,
                """ "reasonReference":[{"reference":"urn:uuid:not-here"}] """))));

        Assert.Equal("Procedure.reasonReference[0] = 'urn:uuid:not-here'", finding.Actual);
    }

    [Fact]
    public void Matching_stage_group_and_member_subjects_produce_no_mismatch() =>
        Assert.Empty(MismatchFindings(QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            QualityFixtures.PrimaryCancerCondition(),
            QualityFixtures.StageGroup(NormalizationFixtures.HasMember("Observation/stage-t-001")),
            QualityFixtures.PrimaryTumour())));

    [Fact]
    public void A_member_resolving_to_a_different_patient_is_reported_against_the_stage_group()
    {
        IngestedBundle ingested = NormalizationFixtures.Ingest(
            SyntheticFixtures.Phase4Bundle("bundle-staging-subject-mismatch"));

        Finding finding = Assert.Single(
            MismatchFindings(QualityFixtures.Assess(ingested.SourceResources)));

        SourceResource stageGroup = ingested.SourceResources
            .Single(source => source.SourceLogicalId == "staging-group-001");

        Assert.Equal(FindingCategory.ReferentialIntegrity, finding.Category);
        Assert.Equal(FindingSeverity.Error, finding.Severity);
        Assert.Equal(stageGroup.Id.Value, finding.Target.Id);
        Assert.Equal(
            "stage group subject 'urn:uuid:aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa'; "
                + "member 'Observation/staging-t-001' subject "
                + "'urn:uuid:aaaaaaaa-1111-4111-8111-bbbbbbbbbbbb'",
            finding.Actual);
    }

    [Fact]
    public void An_unresolved_stage_group_subject_does_not_also_become_a_mismatch()
    {
        SourceQualityAssessment assessment = QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            QualityFixtures.PrimaryCancerCondition(),
            StagingFixtures.StageGroupEntry(
                QualityFixtures.StagingMethod,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject("urn:uuid:no-such-patient"),
                NormalizationFixtures.HasMember("Observation/stage-t-001")),
            StagingFixtures.PrimaryTumourEntry());

        Assert.Empty(MismatchFindings(assessment));
        Assert.Single(UnresolvedFindings(assessment));
    }

    [Fact]
    public void An_unresolved_member_subject_does_not_also_become_a_mismatch()
    {
        SourceQualityAssessment assessment = QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            QualityFixtures.PrimaryCancerCondition(),
            QualityFixtures.StageGroup(NormalizationFixtures.HasMember("Observation/stage-t-001")),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                "stage-t-001",
                NormalizationFixtures.ClinicalPrimaryTumourCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject("urn:uuid:no-such-patient"),
                NormalizationFixtures.StagingValue("T2")));

        Assert.Empty(MismatchFindings(assessment));
        Assert.Single(UnresolvedFindings(assessment));
    }

    [Fact]
    public void An_unrelated_member_observation_is_not_treated_as_a_TNM_category()
    {
        SourceQualityAssessment assessment = QualityFixtures.AssessEntries(
            QualityFixtures.PatientEntry(),
            NormalizationFixtures.PatientEntry(SecondPatientFullUrl, "patient-002"),
            QualityFixtures.PrimaryCancerCondition(),
            QualityFixtures.StageGroup(NormalizationFixtures.HasMember("Observation/prognostic-001")),
            StagingFixtures.ObservationEntry(
                "urn:uuid:77777777-7777-4777-8777-777777777777",
                "prognostic-001",
                "75620-5",
                NormalizationFixtures.Subject(SecondPatientFullUrl)));

        Assert.Empty(MismatchFindings(assessment));
    }
}
