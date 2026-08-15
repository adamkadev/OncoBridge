using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class DerivedIdentityTests
{
    private static SourceResource SourceOfType(IngestedBundle ingested, string resourceType) =>
        ingested.SourceResources.Single(source => source.ResourceType == resourceType);

    [Fact]
    public void Normalizing_the_same_source_resources_twice_yields_the_same_patient_id()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestPrimaryCancerBundle();

        NormalizationResult first = NormalizationFixtures.Normalize(ingested.SourceResources);
        NormalizationResult second = NormalizationFixtures.Normalize(ingested.SourceResources);

        Assert.Equal(first.Patients[0].Id, second.Patients[0].Id);
    }

    [Fact]
    public void Normalizing_the_same_source_resources_twice_yields_the_same_diagnosis_id()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestPrimaryCancerBundle();

        NormalizationResult first = NormalizationFixtures.Normalize(ingested.SourceResources);
        NormalizationResult second = NormalizationFixtures.Normalize(ingested.SourceResources);

        Assert.Equal(first.PrimaryCancerDiagnoses[0].Id, second.PrimaryCancerDiagnoses[0].Id);
        Assert.Equal(first.PrimaryCancerDiagnoses[0].PatientId, second.PrimaryCancerDiagnoses[0].PatientId);
    }

    [Fact]
    public void A_patient_id_derives_from_the_resolved_patient_source_resource()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestPrimaryCancerBundle();

        NormalizationResult result = NormalizationFixtures.Normalize(ingested.SourceResources);

        Assert.Equal(SourceOfType(ingested, "Patient").Id.Value, result.Patients[0].Id.Value);
    }

    [Fact]
    public void A_diagnosis_id_derives_from_its_condition_source_resource()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestPrimaryCancerBundle();

        NormalizationResult result = NormalizationFixtures.Normalize(ingested.SourceResources);

        Assert.Equal(SourceOfType(ingested, "Condition").Id.Value, result.PrimaryCancerDiagnoses[0].Id);
    }

    [Fact]
    public void Lineage_names_the_same_derived_entity_ids_on_every_run()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestPrimaryCancerBundle();

        NormalizationResult first = NormalizationFixtures.Normalize(ingested.SourceResources);
        NormalizationResult second = NormalizationFixtures.Normalize(ingested.SourceResources);

        Assert.Equal(
            first.Lineage.Select(record => (record.DomainEntityType, record.DomainEntityId)),
            second.Lineage.Select(record => (record.DomainEntityType, record.DomainEntityId)));

        Assert.Contains(
            first.Lineage,
            record => record.DomainEntityId == first.Patients[0].Id.Value
                && record.SourceResourceId == SourceOfType(ingested, "Patient").Id);
    }

    [Fact]
    public void Two_diagnoses_for_one_patient_keep_one_derived_patient_id_and_distinct_diagnosis_ids()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestEntries(
            NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, "patient-001"),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                "urn:uuid:condition-a",
                "condition-a",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode),
            NormalizationFixtures.PrimaryCancerConditionEntry(
                "urn:uuid:condition-b",
                "condition-b",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.BreastCancerCode));

        NormalizationResult result = NormalizationFixtures.Normalize(ingested.SourceResources);

        Assert.Single(result.Patients);
        Assert.Equal(2, result.PrimaryCancerDiagnoses.Count);
        Assert.Equal(
            2, result.PrimaryCancerDiagnoses.Select(diagnosis => diagnosis.Id).Distinct().Count());
        Assert.All(
            result.PrimaryCancerDiagnoses,
            diagnosis => Assert.Equal(result.Patients[0].Id, diagnosis.PatientId));
    }

    [Fact]
    public void Ingesting_the_same_payload_again_is_a_different_import_with_different_derived_ids()
    {
        NormalizationResult first = NormalizationFixtures.NormalizePrimaryCancerBundle();
        NormalizationResult second = NormalizationFixtures.NormalizePrimaryCancerBundle();

        Assert.NotEqual(first.Patients[0].Id, second.Patients[0].Id);
        Assert.NotEqual(first.PrimaryCancerDiagnoses[0].Id, second.PrimaryCancerDiagnoses[0].Id);
    }
}
