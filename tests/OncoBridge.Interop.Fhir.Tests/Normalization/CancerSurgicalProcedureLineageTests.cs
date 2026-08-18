using OncoBridge.Application.Imports;
using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class CancerSurgicalProcedureLineageTests
{
    private const string CancerSurgicalProcedure = "CancerSurgicalProcedure";

    private static (IngestedPayload Ingested, NormalizationResult Result) NormalizeFixture()
    {
        IngestedPayload ingested = NormalizationFixtures.IngestSurgicalProcedureBundle();

        return (ingested, NormalizationFixtures.Normalize(ingested.SourceResources));
    }

    private static SourceResource ProcedureSource(IngestedPayload ingested) =>
        ingested.SourceResources.Single(source => source.ResourceType == "Procedure");

    [Fact]
    public void A_procedure_id_derives_from_its_procedure_source_resource()
    {
        (IngestedPayload ingested, NormalizationResult result) = NormalizeFixture();

        Assert.Equal(
            ProcedureSource(ingested).Id.Value,
            Assert.Single(result.CancerSurgicalProcedures).Id);
    }

    [Fact]
    public void Normalizing_the_same_source_resources_twice_yields_the_same_procedure_id()
    {
        IngestedPayload ingested = NormalizationFixtures.IngestSurgicalProcedureBundle();

        NormalizationResult first = NormalizationFixtures.Normalize(ingested.SourceResources);
        NormalizationResult second = NormalizationFixtures.Normalize(ingested.SourceResources);

        Assert.Equal(first.CancerSurgicalProcedures[0].Id, second.CancerSurgicalProcedures[0].Id);
        Assert.Equal(
            first.CancerSurgicalProcedures[0].PatientId, second.CancerSurgicalProcedures[0].PatientId);
    }

    [Fact]
    public void Ingesting_the_same_procedure_payload_again_derives_a_different_procedure_id()
    {
        NormalizationResult first = NormalizationFixtures.NormalizeSurgicalProcedureBundle();
        NormalizationResult second = NormalizationFixtures.NormalizeSurgicalProcedureBundle();

        Assert.NotEqual(first.CancerSurgicalProcedures[0].Id, second.CancerSurgicalProcedures[0].Id);
    }

    [Fact]
    public void A_normalized_procedure_produces_exactly_one_entity_level_lineage_record()
    {
        (IngestedPayload ingested, NormalizationResult result) = NormalizeFixture();

        Lineage lineage = Assert.Single(
            result.Lineage, record => record.DomainEntityType == CancerSurgicalProcedure);

        Assert.Equal(Assert.Single(result.CancerSurgicalProcedures).Id, lineage.DomainEntityId);
        Assert.Equal(ProcedureSource(ingested).Id, lineage.SourceResourceId);
        Assert.True(lineage.IsWholeEntity);
    }

    [Fact]
    public void Procedure_lineage_holds_no_field_level_record()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Assert.All(result.Lineage, lineage => Assert.Null(lineage.FieldPath));
    }

    [Fact]
    public void Every_procedure_lineage_record_names_its_transformation_and_version()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Assert.All(
            result.Lineage.Where(record => record.DomainEntityType == CancerSurgicalProcedure),
            lineage =>
            {
                Assert.Equal("FhirCancerSurgicalProcedureNormalization", lineage.TransformationName);
                Assert.Equal("1.0.0", lineage.TransformationVersion);
            });
    }

    [Fact]
    public void Two_procedures_for_one_patient_keep_one_patient_lineage_row_and_one_row_each()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeEntries(
            ProcedureFixtures.PatientEntry(),
            NormalizationFixtures.SurgicalProcedureEntry(
                "urn:uuid:procedure-a",
                "procedure-a",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.LumpectomyCode),
            NormalizationFixtures.SurgicalProcedureEntry(
                "urn:uuid:procedure-b",
                "procedure-b",
                NormalizationFixtures.PatientFullUrl,
                NormalizationFixtures.LumpectomyCode));

        Assert.Equal(2, result.CancerSurgicalProcedures.Count);
        Assert.Equal(
            2, result.CancerSurgicalProcedures.Select(procedure => procedure.Id).Distinct().Count());
        Assert.Single(result.Lineage, record => record.DomainEntityType == "Patient");
        Assert.Equal(
            2, result.Lineage.Count(record => record.DomainEntityType == CancerSurgicalProcedure));
    }

    [Fact]
    public void The_whole_procedure_bundle_records_one_lineage_row_per_normalized_entity()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Assert.Single(result.Lineage, record => record.DomainEntityType == "Patient");
        Assert.Single(result.Lineage, record => record.DomainEntityType == CancerSurgicalProcedure);
        Assert.Equal(2, result.Lineage.Count);
    }
}
