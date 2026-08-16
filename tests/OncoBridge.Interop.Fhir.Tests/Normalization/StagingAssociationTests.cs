using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class StagingAssociationTests
{
    private const string OtherPatientFullUrl = "urn:uuid:other-patient";

    private const string OtherConditionFullUrl = "urn:uuid:other-condition";

    private const string SecondConditionFullUrl = "urn:uuid:second-condition";

    private const string SecondStageGroupFullUrl = "urn:uuid:second-stage-group";

    private const string SecondStageGroupValue = "IIIB";

    private const string UnusableConditionCode =
        """ "code":{"text":"Breast cancer, stated only as free text"} """;

    private static PrimaryCancerDiagnosisId DiagnosisIdOf(IngestedBundle ingested, string logicalId) =>
        new(ingested.SourceResources.Single(source => source.SourceLogicalId == logicalId).Id.Value);

    private static string OtherPatientEntry() =>
        NormalizationFixtures.PatientEntry(OtherPatientFullUrl, "patient-002");

    private static string OtherConditionEntry() =>
        NormalizationFixtures.PrimaryCancerConditionEntry(
            OtherConditionFullUrl,
            "condition-002",
            OtherPatientFullUrl,
            NormalizationFixtures.BreastCancerCode);

    [Fact]
    public void A_focus_stated_as_a_full_url_resolves_to_the_condition_being_staged()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue))).CancerStagings);

        Assert.NotNull(staging.StageGroup);
    }

    [Fact]
    public void A_focus_stated_as_a_relative_condition_reference_resolves()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus("Condition/" + StagingFixtures.ConditionLogicalId),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue))).CancerStagings);

        Assert.NotNull(staging.StageGroup);
    }

    [Fact]
    public void A_stage_group_without_a_focus_stages_nothing()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Empty(result.CancerStagings);
        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_focus_that_cannot_be_resolved_stages_nothing()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus("urn:uuid:condition-that-is-not-here"),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Empty(result.CancerStagings);
    }

    [Fact]
    public void A_focus_naming_two_different_conditions_is_ambiguous_and_stages_nothing()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            StagingFixtures.PatientEntry(),
            StagingFixtures.ConditionEntry(),
            OtherPatientEntry(),
            OtherConditionEntry(),
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(
                    NormalizationFixtures.ConditionFullUrl, OtherConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Empty(result.CancerStagings);
    }

    [Fact]
    public void A_focus_never_resolves_to_a_condition_from_another_batch()
    {
        IngestedBundle conditionBatch = NormalizationFixtures.IngestEntries(
            StagingFixtures.PatientEntry(), StagingFixtures.ConditionEntry());

        IngestedBundle stagingBatch = NormalizationFixtures.IngestEntries(
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.NotEqual(conditionBatch.Batch.Id, stagingBatch.Batch.Id);

        SourceResource[] bothBatches =
            [.. conditionBatch.SourceResources, .. stagingBatch.SourceResources];

        NormalizationResult result = NormalizationFixtures.Normalize(bothBatches);

        Assert.Empty(result.CancerStagings);
        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_focus_on_a_condition_that_is_not_a_primary_cancer_condition_stages_nothing()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            StagingFixtures.PatientEntry(),
            NormalizationFixtures.ConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                StagingFixtures.ConditionLogicalId,
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                NormalizationFixtures.BreastCancerCode),
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Empty(result.CancerStagings);
        Assert.Empty(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_stage_group_subject_naming_another_patient_than_the_condition_stages_nothing()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            StagingFixtures.PatientEntry(),
            StagingFixtures.ConditionEntry(),
            OtherPatientEntry(),
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject(OtherPatientFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Empty(result.CancerStagings);
    }

    [Fact]
    public void A_stage_group_subject_that_cannot_be_resolved_does_not_reject_the_assessment()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject("urn:uuid:patient-that-is-not-here"),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue))).CancerStagings);

        Assert.NotNull(staging.StageGroup);
    }

    [Fact]
    public void The_staging_patient_is_the_one_the_resolved_condition_names()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestEntries(
            StagingFixtures.PatientEntry(),
            StagingFixtures.ConditionEntry(),
            OtherPatientEntry(),
            OtherConditionEntry(),
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(OtherConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        NormalizationResult result = NormalizationFixtures.Normalize(ingested.SourceResources);

        CancerStaging staging = Assert.Single(result.CancerStagings);
        SourceResource otherPatient =
            ingested.SourceResources.Single(source => source.SourceLogicalId == "patient-002");

        Assert.Equal(2, result.Patients.Count);
        Assert.Equal(otherPatient.Id.Value, staging.PatientId.Value);
    }

    [Fact]
    public void An_eligible_condition_that_yielded_no_diagnosis_is_not_staged()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            StagingFixtures.PatientEntry(),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                StagingFixtures.ConditionLogicalId,
                NormalizationFixtures.PatientFullUrl,
                UnusableConditionCode),
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Empty(result.PrimaryCancerDiagnoses);
        Assert.Empty(result.CancerStagings);
        Assert.DoesNotContain(result.Lineage, record => record.DomainEntityType == "CancerStaging");
    }

    [Fact]
    public void One_undiagnosable_condition_does_not_stop_the_other_assessments_in_the_batch()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestEntries(
            StagingFixtures.PatientEntry(),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                SecondConditionFullUrl,
                "condition-002",
                NormalizationFixtures.PatientFullUrl,
                UnusableConditionCode),
            StagingFixtures.ObservationEntry(
                SecondStageGroupFullUrl,
                "stage-group-002",
                NormalizationFixtures.PathologicalStageGroupCode,
                NormalizationFixtures.Focus(SecondConditionFullUrl),
                NormalizationFixtures.StagingValue(SecondStageGroupValue)),
            StagingFixtures.ConditionEntry(),
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        NormalizationResult result = NormalizationFixtures.Normalize(ingested.SourceResources);

        PrimaryCancerDiagnosis diagnosis = Assert.Single(result.PrimaryCancerDiagnoses);
        CancerStaging staging = Assert.Single(result.CancerStagings);

        Assert.Equal(DiagnosisIdOf(ingested, StagingFixtures.ConditionLogicalId), diagnosis.Id);
        Assert.Equal(diagnosis.Id, staging.PrimaryCancerDiagnosisId);
        Assert.Equal(StagingFixtures.StageGroupValue, staging.StageGroup!.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Every_staging_names_a_diagnosis_the_same_result_emitted(bool includeUndiagnosable)
    {
        string[] undiagnosable = includeUndiagnosable
            ?
            [
                NormalizationFixtures.PrimaryCancerConditionEntry(
                    SecondConditionFullUrl,
                    "condition-002",
                    NormalizationFixtures.PatientFullUrl,
                    UnusableConditionCode),
                StagingFixtures.ObservationEntry(
                    SecondStageGroupFullUrl,
                    "stage-group-002",
                    NormalizationFixtures.PathologicalStageGroupCode,
                    NormalizationFixtures.Focus(SecondConditionFullUrl),
                    NormalizationFixtures.StagingValue(SecondStageGroupValue)),
            ]
            : [];

        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
        [
            StagingFixtures.PatientEntry(),
            StagingFixtures.ConditionEntry(),
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)),
            .. undiagnosable,
        ]);

        Assert.NotEmpty(result.CancerStagings);
        Assert.All(
            result.CancerStagings,
            staging => Assert.Contains(
                result.PrimaryCancerDiagnoses,
                diagnosis => diagnosis.Id == staging.PrimaryCancerDiagnosisId));
    }

    [Fact]
    public void A_staging_names_the_diagnosis_its_focus_condition_produced()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeTnmStagingBundle();

        CancerStaging staging = Assert.Single(result.CancerStagings);
        PrimaryCancerDiagnosis diagnosis = Assert.Single(result.PrimaryCancerDiagnoses);

        Assert.Equal(diagnosis.Id, staging.PrimaryCancerDiagnosisId);
        Assert.Equal(diagnosis.PatientId, staging.PatientId);
    }

    [Fact]
    public void Two_primary_cancers_for_one_patient_are_staged_against_their_own_diagnoses()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestEntries(
            StagingFixtures.PatientEntry(),
            StagingFixtures.ConditionEntry(),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                SecondConditionFullUrl,
                "condition-002",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode),
            StagingFixtures.StageGroupEntry(
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)),
            StagingFixtures.ObservationEntry(
                SecondStageGroupFullUrl,
                "stage-group-002",
                NormalizationFixtures.PathologicalStageGroupCode,
                NormalizationFixtures.Focus(SecondConditionFullUrl),
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                NormalizationFixtures.StagingValue(SecondStageGroupValue)));

        NormalizationResult result = NormalizationFixtures.Normalize(ingested.SourceResources);

        CancerStaging first = Assert.Single(
            result.CancerStagings, staging => staging.StageGroup!.Code == StagingFixtures.StageGroupValue);

        CancerStaging second = Assert.Single(
            result.CancerStagings, staging => staging.StageGroup!.Code == SecondStageGroupValue);

        Assert.Single(result.Patients);
        Assert.Equal(2, result.PrimaryCancerDiagnoses.Count);
        Assert.Equal(first.PatientId, second.PatientId);

        Assert.Equal(
            DiagnosisIdOf(ingested, StagingFixtures.ConditionLogicalId), first.PrimaryCancerDiagnosisId);
        Assert.Equal(DiagnosisIdOf(ingested, "condition-002"), second.PrimaryCancerDiagnosisId);
        Assert.NotEqual(first.PrimaryCancerDiagnosisId, second.PrimaryCancerDiagnosisId);

        Assert.All(
            result.CancerStagings,
            staging => Assert.Contains(
                result.PrimaryCancerDiagnoses,
                diagnosis => diagnosis.Id == staging.PrimaryCancerDiagnosisId));
    }

    [Fact]
    public void A_category_whose_focus_names_another_condition_is_not_attached()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            StagingFixtures.PatientEntry(),
            StagingFixtures.ConditionEntry(),
            OtherPatientEntry(),
            OtherConditionEntry(),
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                NormalizationFixtures.ClinicalPrimaryTumourCode,
                NormalizationFixtures.Focus(OtherConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.PrimaryTumourValue)));

        Assert.Empty(Assert.Single(result.CancerStagings).Categories);
    }

    [Fact]
    public void A_category_whose_subject_names_another_patient_is_not_attached()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            StagingFixtures.PatientEntry(),
            StagingFixtures.ConditionEntry(),
            OtherPatientEntry(),
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                NormalizationFixtures.ClinicalPrimaryTumourCode,
                NormalizationFixtures.Subject(OtherPatientFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.PrimaryTumourValue)));

        Assert.Empty(Assert.Single(result.CancerStagings).Categories);
    }

    [Fact]
    public void A_category_without_a_focus_is_still_attached_because_hasMember_already_composed_it()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                NormalizationFixtures.ClinicalPrimaryTumourCode,
                NormalizationFixtures.StagingValue(
                    StagingFixtures.PrimaryTumourValue))).CancerStagings);

        Assert.NotNull(staging.PrimaryTumour);
    }
}
