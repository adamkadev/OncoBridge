using OncoBridge.Application.Imports;
using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class StagingAggregationTests
{
    [Fact]
    public void A_category_observation_the_stage_group_never_references_is_not_attached()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.PrimaryTumourEntry(),
            StagingFixtures.RegionalNodesEntry(),
            StagingFixtures.DistantMetastasesEntry()).CancerStagings);

        Assert.NotNull(staging.PrimaryTumour);
        Assert.Null(staging.RegionalNodes);
        Assert.Null(staging.DistantMetastases);
    }

    [Fact]
    public void Two_stage_groups_sharing_one_condition_each_keep_only_their_own_members()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.ObservationEntry(
                "urn:uuid:stage-group-pathological",
                "stage-group-002",
                NormalizationFixtures.PathologicalStageGroupCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                NormalizationFixtures.HasMember(NormalizationFixtures.RegionalNodesFullUrl)),
            StagingFixtures.PrimaryTumourEntry(),
            StagingFixtures.RegionalNodesEntry());

        Assert.Equal(2, result.CancerStagings.Count);
        Assert.All(result.CancerStagings, staging => Assert.Single(staging.Categories));
        Assert.Single(result.CancerStagings, staging => staging.PrimaryTumour is not null);
        Assert.Single(result.CancerStagings, staging => staging.RegionalNodes is not null);
    }

    [Fact]
    public void A_member_referenced_by_its_full_url_resolves()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.PrimaryTumourEntry()).CancerStagings);

        Assert.NotNull(staging.PrimaryTumour);
    }

    [Fact]
    public void A_member_referenced_by_its_logical_id_resolves()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.HasMember(
                    "Observation/" + StagingFixtures.PrimaryTumourLogicalId)),
            StagingFixtures.PrimaryTumourEntry()).CancerStagings);

        Assert.NotNull(staging.PrimaryTumour);
    }

    [Fact]
    public void A_member_that_cannot_be_resolved_does_not_discard_its_resolvable_siblings()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.HasMember(
                    "urn:uuid:member-that-is-not-here",
                    NormalizationFixtures.PrimaryTumourFullUrl,
                    NormalizationFixtures.RegionalNodesFullUrl)),
            StagingFixtures.PrimaryTumourEntry(),
            StagingFixtures.RegionalNodesEntry()).CancerStagings);

        Assert.NotNull(staging.PrimaryTumour);
        Assert.NotNull(staging.RegionalNodes);
    }

    [Fact]
    public void A_member_never_resolves_to_an_observation_from_another_batch()
    {
        IngestedPayload memberBatch = NormalizationFixtures.IngestEntries(
            StagingFixtures.PrimaryTumourEntry());

        IngestedPayload stagingBatch = NormalizationFixtures.IngestEntries(
            StagingFixtures.PatientEntry(),
            StagingFixtures.ConditionEntry(),
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)));

        Assert.NotEqual(memberBatch.Batch.Id, stagingBatch.Batch.Id);

        SourceResource[] bothBatches =
            [.. memberBatch.SourceResources, .. stagingBatch.SourceResources];

        CancerStaging staging =
            Assert.Single(NormalizationFixtures.Normalize(bothBatches).CancerStagings);

        Assert.Empty(staging.Categories);
    }

    [Fact]
    public void The_same_member_referenced_twice_contributes_one_category()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.HasMember(
                    NormalizationFixtures.PrimaryTumourFullUrl,
                    "Observation/" + StagingFixtures.PrimaryTumourLogicalId)),
            StagingFixtures.PrimaryTumourEntry()).CancerStagings);

        Assert.Single(staging.Categories);
        Assert.Single(staging.CategorySourceResources);
    }

    [Fact]
    public void Two_distinct_categories_on_one_axis_make_the_assessment_ambiguous_and_it_is_skipped()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(
                    NormalizationFixtures.PrimaryTumourFullUrl, "urn:uuid:second-primary-tumour")),
            StagingFixtures.PrimaryTumourEntry(),
            StagingFixtures.CategoryEntry(
                "urn:uuid:second-primary-tumour",
                "stage-t-002",
                NormalizationFixtures.PathologicalPrimaryTumourCode,
                "T3"));

        Assert.Empty(result.CancerStagings);
    }

    [Fact]
    public void An_ambiguous_assessment_does_not_stop_the_other_assessments_in_the_batch()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(
                    NormalizationFixtures.PrimaryTumourFullUrl, "urn:uuid:second-primary-tumour")),
            StagingFixtures.ObservationEntry(
                "urn:uuid:stage-group-pathological",
                "stage-group-002",
                NormalizationFixtures.PathologicalStageGroupCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                NormalizationFixtures.HasMember(NormalizationFixtures.RegionalNodesFullUrl)),
            StagingFixtures.PrimaryTumourEntry(),
            StagingFixtures.CategoryEntry(
                "urn:uuid:second-primary-tumour",
                "stage-t-002",
                NormalizationFixtures.PathologicalPrimaryTumourCode,
                "T3"),
            StagingFixtures.RegionalNodesEntry());

        CancerStaging staging = Assert.Single(result.CancerStagings);

        Assert.NotNull(staging.RegionalNodes);
        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void An_ambiguous_assessment_never_silently_selects_one_of_the_competing_categories()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(
                    NormalizationFixtures.PrimaryTumourFullUrl,
                    "urn:uuid:second-primary-tumour",
                    NormalizationFixtures.RegionalNodesFullUrl)),
            StagingFixtures.PrimaryTumourEntry(),
            StagingFixtures.CategoryEntry(
                "urn:uuid:second-primary-tumour",
                "stage-t-002",
                NormalizationFixtures.PathologicalPrimaryTumourCode,
                "T3"),
            StagingFixtures.RegionalNodesEntry());

        Assert.Empty(result.CancerStagings);
        Assert.DoesNotContain(result.Lineage, record => record.DomainEntityType == "CancerStaging");
    }
}
