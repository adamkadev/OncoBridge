using OncoBridge.Application.Imports;
using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class NormalizationLineageTests
{
    private static (IngestedPayload Ingested, NormalizationResult Result) NormalizeFixture()
    {
        IngestedPayload ingested = NormalizationFixtures.IngestPrimaryCancerBundle();

        return (ingested, NormalizationFixtures.Normalize(ingested.SourceResources));
    }

    private static SourceResource SourceOfType(IngestedPayload ingested, string resourceType) =>
        ingested.SourceResources.Single(source => source.ResourceType == resourceType);

    [Fact]
    public void A_normalized_patient_produces_exactly_one_entity_level_lineage_record()
    {
        (IngestedPayload ingested, NormalizationResult result) = NormalizeFixture();

        Lineage lineage = Assert.Single(result.Lineage, record => record.DomainEntityType == "Patient");

        Assert.Equal(result.Patients[0].Id.Value, lineage.DomainEntityId);
        Assert.Equal(SourceOfType(ingested, "Patient").Id, lineage.SourceResourceId);
        Assert.True(lineage.IsWholeEntity);
    }

    [Fact]
    public void A_normalized_diagnosis_produces_exactly_one_entity_level_lineage_record()
    {
        (IngestedPayload ingested, NormalizationResult result) = NormalizeFixture();

        Lineage lineage = Assert.Single(
            result.Lineage, record => record.DomainEntityType == "PrimaryCancerDiagnosis");

        Assert.Equal(result.PrimaryCancerDiagnoses[0].Id.Value, lineage.DomainEntityId);
        Assert.Equal(SourceOfType(ingested, "Condition").Id, lineage.SourceResourceId);
        Assert.True(lineage.IsWholeEntity);
    }

    [Fact]
    public void A_bundle_carrying_no_staging_produces_no_field_level_lineage()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Assert.NotEmpty(result.Lineage);
        Assert.All(result.Lineage, lineage => Assert.Null(lineage.FieldPath));
    }

    [Fact]
    public void Every_lineage_record_names_its_transformation_and_version()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Assert.All(
            result.Lineage,
            lineage =>
            {
                Assert.False(string.IsNullOrWhiteSpace(lineage.TransformationName));
                Assert.Equal("1.0.0", lineage.TransformationVersion);
            });
    }

    [Fact]
    public void A_patient_shared_by_two_diagnoses_still_produces_one_patient_lineage_record()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
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

        Assert.Single(result.Lineage, record => record.DomainEntityType == "Patient");
        Assert.Equal(
            2, result.Lineage.Count(record => record.DomainEntityType == "PrimaryCancerDiagnosis"));
    }
}
