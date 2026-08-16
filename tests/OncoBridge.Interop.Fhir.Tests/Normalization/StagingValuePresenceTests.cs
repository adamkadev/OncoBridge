using OncoBridge.Domain.Oncology;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class StagingValuePresenceTests
{
    [Fact]
    public void A_stage_group_without_a_value_still_stages_when_a_category_is_usable()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.PrimaryTumourEntry()).CancerStagings);

        Assert.Null(staging.StageGroup);
        Assert.NotNull(staging.PrimaryTumour);
    }

    [Fact]
    public void A_stage_group_value_with_no_usable_coding_is_not_mapped()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                """ "valueCodeableConcept":{"text":"Stage IIA, stated only as free text"} """,
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.PrimaryTumourEntry()).CancerStagings);

        Assert.Null(staging.StageGroup);
        Assert.NotNull(staging.PrimaryTumour);
    }

    [Fact]
    public void A_non_coded_stage_group_value_is_never_read_as_a_stage_group()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                """ "valueString":"IIA" """,
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.PrimaryTumourEntry()).CancerStagings);

        Assert.Null(staging.StageGroup);
    }

    [Fact]
    public void A_category_observation_without_a_value_contributes_no_category()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(
                    NormalizationFixtures.PrimaryTumourFullUrl,
                    NormalizationFixtures.RegionalNodesFullUrl)),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                NormalizationFixtures.ClinicalPrimaryTumourCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl)),
            StagingFixtures.RegionalNodesEntry()).CancerStagings);

        Assert.Null(staging.PrimaryTumour);
        Assert.NotNull(staging.RegionalNodes);
    }

    [Fact]
    public void A_category_value_is_never_fabricated_from_the_observation_code()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                NormalizationFixtures.ClinicalPrimaryTumourCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                """ "valueCodeableConcept":{"coding":[{"display":"T2"}]} """)).CancerStagings);

        Assert.Empty(staging.Categories);
        Assert.NotNull(staging.StageGroup);
    }

    [Fact]
    public void A_stage_group_with_neither_a_value_nor_a_usable_category_stages_nothing()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry());

        Assert.Empty(result.CancerStagings);
        Assert.Single(result.PrimaryCancerDiagnoses);
    }

    [Fact]
    public void A_stage_group_whose_only_member_is_unusable_stages_nothing()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                NormalizationFixtures.ClinicalPrimaryTumourCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl)));

        Assert.Empty(result.CancerStagings);
        Assert.DoesNotContain(result.Lineage, record => record.DomainEntityType == "CancerStaging");
    }

    [Fact]
    public void A_category_code_preserves_system_code_and_display_exactly()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                NormalizationFixtures.ClinicalPrimaryTumourCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(
                    NormalizationFixtures.StagingCodeSystem, "T2", "T2 stated by the source"))
            ).CancerStagings);

        Assert.Equal(NormalizationFixtures.StagingCodeSystem, staging.PrimaryTumour!.Code.System);
        Assert.Equal("T2", staging.PrimaryTumour.Code.Code);
        Assert.Equal("T2 stated by the source", staging.PrimaryTumour.Code.Display);
    }
}
