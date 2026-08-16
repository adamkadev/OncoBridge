using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class StagingLineageTests
{
    private const string CancerStaging = "CancerStaging";

    private static (IngestedBundle Ingested, NormalizationResult Result) NormalizeFixture()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestTnmStagingBundle();

        return (ingested, NormalizationFixtures.Normalize(ingested.SourceResources));
    }

    private static SourceResourceId SourceOf(IngestedBundle ingested, string logicalId) =>
        ingested.SourceResources.Single(source => source.SourceLogicalId == logicalId).Id;

    private static Lineage[] StagingLineage(NormalizationResult result) =>
        [.. result.Lineage.Where(record => record.DomainEntityType == CancerStaging)];

    [Fact]
    public void A_complete_assessment_produces_one_entity_record_and_one_record_per_category()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Lineage[] lineage = StagingLineage(result);

        Assert.Equal(4, lineage.Length);
        Assert.Single(lineage, record => record.IsWholeEntity);
        Assert.Equal(3, lineage.Count(record => !record.IsWholeEntity));
    }

    [Fact]
    public void The_entity_level_record_names_the_stage_group_observation_as_the_root()
    {
        (IngestedBundle ingested, NormalizationResult result) = NormalizeFixture();

        Lineage lineage = Assert.Single(StagingLineage(result), record => record.IsWholeEntity);

        Assert.Equal(result.CancerStagings[0].Id, lineage.DomainEntityId);
        Assert.Equal(SourceOf(ingested, "staging-group-001"), lineage.SourceResourceId);
    }

    [Theory]
    [InlineData("PrimaryTumour", "staging-t-001")]
    [InlineData("RegionalNodes", "staging-n-001")]
    [InlineData("DistantMetastases", "staging-m-001")]
    public void Each_axis_records_the_member_observation_it_was_read_from(
        string fieldPath, string logicalId)
    {
        (IngestedBundle ingested, NormalizationResult result) = NormalizeFixture();

        Lineage lineage = Assert.Single(StagingLineage(result), record => record.FieldPath == fieldPath);

        Assert.Equal(result.CancerStagings[0].Id, lineage.DomainEntityId);
        Assert.Equal(SourceOf(ingested, logicalId), lineage.SourceResourceId);
    }

    [Fact]
    public void No_field_level_record_is_written_for_the_stage_group_method_or_effective_date()
    {
        (IngestedBundle ingested, NormalizationResult result) = NormalizeFixture();

        SourceResourceId stageGroup = SourceOf(ingested, "staging-group-001");

        Assert.Single(StagingLineage(result), record => record.SourceResourceId == stageGroup);
    }

    [Fact]
    public void Every_staging_lineage_record_names_its_transformation_and_version()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Assert.All(
            StagingLineage(result),
            lineage =>
            {
                Assert.Equal("FhirCancerStagingNormalization", lineage.TransformationName);
                Assert.Equal("1.0.0", lineage.TransformationVersion);
            });
    }

    [Fact]
    public void Staging_lineage_holds_no_duplicate_rows()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Lineage[] lineage = StagingLineage(result);

        Assert.Equal(
            lineage.Length,
            lineage.Select(record => (record.DomainEntityId, record.FieldPath, record.SourceResourceId))
                .Distinct()
                .Count());
    }

    [Fact]
    public void An_assessment_holding_only_a_stage_group_produces_the_entity_record_alone()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Lineage lineage = Assert.Single(StagingLineage(result));

        Assert.True(lineage.IsWholeEntity);
    }

    [Fact]
    public void The_whole_batch_records_one_lineage_row_per_entity_and_per_contributing_category()
    {
        (_, NormalizationResult result) = NormalizeFixture();

        Assert.Single(result.Lineage, record => record.DomainEntityType == "Patient");
        Assert.Single(result.Lineage, record => record.DomainEntityType == "PrimaryCancerDiagnosis");
        Assert.Equal(6, result.Lineage.Count);
    }
}
