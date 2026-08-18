using OncoBridge.Application.Imports;
using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class PatientReferenceResolutionTests
{
    [Fact]
    public void A_urn_uuid_reference_resolves_against_the_entry_full_url()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

        Assert.Single(result.Patients);
        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_relative_patient_reference_resolves_against_the_source_logical_id()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                "Patient/patient-001",
                NormalizationFixtures.BreastCancerCode));

        Assert.Single(result.Patients);
        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void An_unresolved_reference_yields_no_orphan_diagnosis_and_stops_nothing_else()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                "urn:uuid:condition-dangling",
                "condition-dangling",
                "urn:uuid:patient-that-is-not-here",
                NormalizationFixtures.BreastCancerCode),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

        Assert.Single(result.Patients);
        Assert.Single(result.PrimaryCancerDiagnoses);
        Assert.Equal(result.Patients[0].Id, result.PrimaryCancerDiagnoses[0].PatientId);
    }

    [Fact]
    public void A_reference_never_resolves_to_a_patient_from_another_batch()
    {
        IngestedPayload patientBatch = NormalizationFixtures.IngestEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"));

        IngestedPayload conditionBatch = NormalizationFixtures.IngestEntries(
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

        Assert.NotEqual(patientBatch.Batch.Id, conditionBatch.Batch.Id);

        SourceResource[] bothBatches =
            [.. patientBatch.SourceResources, .. conditionBatch.SourceResources];

        NormalizationResult result = NormalizationFixtures.Normalize(bothBatches);

        Assert.Empty(result.Patients);
        Assert.Empty(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void An_ambiguous_reference_is_not_silently_resolved_to_one_of_the_candidates()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-002"),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

        Assert.Empty(result.Patients);
        Assert.Empty(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_condition_without_a_subject_produces_no_diagnosis()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.ConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                $$""" "meta":{"profile":["{{NormalizationFixtures.PrimaryCancerConditionProfile}}"]} """,
                NormalizationFixtures.BreastCancerCode));

        Assert.Empty(result.Patients);
        Assert.Empty(result.PrimaryCancerDiagnoses);
    }
}
