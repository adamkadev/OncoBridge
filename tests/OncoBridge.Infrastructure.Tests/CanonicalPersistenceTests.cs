using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Infrastructure.Persistence;

namespace OncoBridge.Infrastructure.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CanonicalPersistenceTests(PostgreSqlFixture postgres)
{
    private async Task<(NormalizationScenario Scenario, ImportBatchId BatchId)> NormalizedFixtureAsync()
    {
        NormalizationScenario scenario = await NormalizationScenario.StartAsync(postgres);
        ImportBatchId batchId = await scenario.IngestCompleteBundleAsync();

        await scenario.NormalizeAsync(batchId);

        return (scenario, batchId);
    }

    [Fact]
    public async Task The_complete_bundle_persists_one_row_per_canonical_concept()
    {
        (NormalizationScenario scenario, _) = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        Assert.Equal(new CanonicalCounts(1, 1, 1, 3, 1, 7), await scenario.CountsAsync());
    }

    [Fact]
    public async Task Every_canonical_entity_carries_the_expected_lineage_rows()
    {
        (NormalizationScenario scenario, _) = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        List<Lineage> lineage = await scenario.LineageAsync();

        Assert.Single(lineage, record => record.DomainEntityType == "Patient");
        Assert.Single(lineage, record => record.DomainEntityType == "PrimaryCancerDiagnosis");
        Assert.Single(lineage, record => record.DomainEntityType == "CancerSurgicalProcedure");
        Assert.Equal(4, lineage.Count(record => record.DomainEntityType == "CancerStaging"));
        Assert.Equal(3, lineage.Count(record => record.FieldPath is not null));
    }

    [Fact]
    public async Task The_diagnosis_staging_and_procedure_all_reference_the_persisted_patient()
    {
        (NormalizationScenario scenario, _) = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        PatientId patientId = (await scenario.SinglePatientAsync()).Id;

        Assert.Equal(patientId, (await scenario.SingleDiagnosisAsync()).PatientId);
        Assert.Equal(patientId, (await scenario.SingleStagingAsync()).PatientId);
        Assert.Equal(patientId, (await scenario.SingleProcedureAsync()).PatientId);
    }

    [Fact]
    public async Task The_staging_references_the_persisted_primary_cancer_diagnosis()
    {
        (NormalizationScenario scenario, _) = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        Assert.Equal(
            (await scenario.SingleDiagnosisAsync()).Id,
            (await scenario.SingleStagingAsync()).PrimaryCancerDiagnosisId);
    }

    [Fact]
    public async Task The_reloaded_staging_exposes_its_three_axes()
    {
        (NormalizationScenario scenario, _) = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        CancerStaging staging = await scenario.SingleStagingAsync();

        Assert.Equal(3, staging.Categories.Count);
        Assert.Equal("T2", staging.PrimaryTumour!.Code.Code);
        Assert.Equal("N1", staging.RegionalNodes!.Code.Code);
        Assert.Equal("M0", staging.DistantMetastases!.Code.Code);
    }

    [Fact]
    public async Task Each_reloaded_category_still_names_the_observation_it_was_read_from()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        List<SourceResource> sources = await scenario.ReloadSourcesAsync(batchId);
        CancerStaging staging = await scenario.SingleStagingAsync();

        Assert.Equal(
            sources.Single(source => source.SourceLogicalId == "staging-t-001").Id,
            staging.PrimaryTumour!.SourceResourceId);
        Assert.Equal(3, staging.CategorySourceResources.Count);
    }

    [Fact]
    public async Task Canonical_identity_is_derived_from_the_source_resources_that_produced_it()
    {
        (NormalizationScenario scenario, ImportBatchId batchId) = await NormalizedFixtureAsync();
        await using OncoBridgeDbContext _context = scenario.Context;

        List<SourceResource> sources = await scenario.ReloadSourcesAsync(batchId);

        Assert.Equal(
            sources.Single(source => source.ResourceType == "Patient").Id.Value,
            (await scenario.SinglePatientAsync()).Id.Value);
        Assert.Equal(
            sources.Single(source => source.ResourceType == "Condition").Id.Value,
            (await scenario.SingleDiagnosisAsync()).Id.Value);
        Assert.Equal(
            sources.Single(source => source.SourceLogicalId == "staging-group-001").Id.Value,
            (await scenario.SingleStagingAsync()).Id);
        Assert.Equal(
            sources.Single(source => source.ResourceType == "Procedure").Id.Value,
            (await scenario.SingleProcedureAsync()).Id);
    }
}
