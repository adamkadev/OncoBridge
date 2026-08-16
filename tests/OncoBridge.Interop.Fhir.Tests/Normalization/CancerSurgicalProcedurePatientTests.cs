using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class CancerSurgicalProcedurePatientTests
{
    private static string ProcedureSubjectedTo(string reference) =>
        NormalizationFixtures.SurgicalProcedureEntry(
            NormalizationFixtures.ProcedureFullUrl,
            ProcedureFixtures.ProcedureLogicalId,
            reference,
            NormalizationFixtures.LumpectomyCode);

    [Fact]
    public void A_subject_stated_as_a_urn_uuid_resolves_against_the_entry_full_url()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            ProcedureFixtures.PatientEntry(),
            ProcedureSubjectedTo(NormalizationFixtures.PatientFullUrl));

        Patient patient = Assert.Single(result.Patients);

        Assert.Equal(patient.Id, Assert.Single(result.CancerSurgicalProcedures).PatientId);
    }

    [Fact]
    public void A_subject_stated_as_a_relative_reference_resolves_against_the_source_logical_id()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            ProcedureFixtures.PatientEntry(),
            ProcedureSubjectedTo("Patient/" + ProcedureFixtures.PatientLogicalId));

        Patient patient = Assert.Single(result.Patients);

        Assert.Equal(patient.Id, Assert.Single(result.CancerSurgicalProcedures).PatientId);
    }

    [Fact]
    public void An_unresolved_subject_yields_no_procedure_and_stops_nothing_else()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            ProcedureFixtures.PatientEntry(),
            NormalizationFixtures.SurgicalProcedureEntry(
                "urn:uuid:procedure-dangling",
                "procedure-dangling",
                "urn:uuid:patient-that-is-not-here",
                NormalizationFixtures.LumpectomyCode),
            ProcedureFixtures.SurgicalProcedureEntry(NormalizationFixtures.LumpectomyCode));

        Assert.Single(result.CancerSurgicalProcedures);
        Assert.Single(result.Patients);
    }

    [Fact]
    public void A_subject_never_resolves_to_a_patient_from_another_batch()
    {
        IngestedBundle patientBatch =
            NormalizationFixtures.IngestEntries(ProcedureFixtures.PatientEntry());

        IngestedBundle procedureBatch = NormalizationFixtures.IngestEntries(
            ProcedureFixtures.SurgicalProcedureEntry(NormalizationFixtures.LumpectomyCode));

        Assert.NotEqual(patientBatch.Batch.Id, procedureBatch.Batch.Id);

        SourceResource[] bothBatches =
            [.. patientBatch.SourceResources, .. procedureBatch.SourceResources];

        NormalizationResult result = NormalizationFixtures.Normalize(bothBatches);

        Assert.Empty(result.CancerSurgicalProcedures);
        Assert.Empty(result.Patients);
    }

    [Fact]
    public void A_bundle_holding_only_a_procedure_still_normalizes_its_patient()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeSurgicalProcedureBundle();

        Patient patient = Assert.Single(result.Patients);

        Assert.Empty(result.PrimaryCancerDiagnoses);
        Assert.Equal(patient.Id, Assert.Single(result.CancerSurgicalProcedures).PatientId);
    }

    [Fact]
    public void A_patient_already_normalized_by_a_diagnosis_is_not_duplicated_by_a_procedure()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            ProcedureFixtures.PatientEntry(),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                NormalizationFixtures.ConditionFullUrl,
                "condition-001",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode),
            ProcedureFixtures.SurgicalProcedureEntry(NormalizationFixtures.LumpectomyCode));

        Patient patient = Assert.Single(result.Patients);

        Assert.Equal(patient.Id, Assert.Single(result.PrimaryCancerDiagnoses).PatientId);
        Assert.Equal(patient.Id, Assert.Single(result.CancerSurgicalProcedures).PatientId);
        Assert.Single(result.Lineage, record => record.DomainEntityType == "Patient");
    }
}
