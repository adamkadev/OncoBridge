using OncoBridge.Domain.Oncology;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class StagingClassificationTests
{
    private const string UnsupportedObservableLoincCode = "85337-4";

    private static CancerStaging StageGroupCoded(string loincCode)
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.StageGroupFullUrl,
                StagingFixtures.StageGroupLogicalId,
                loincCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        return Assert.Single(result.CancerStagings);
    }

    private static CancerStaging MemberCoded(string loincCode, string value)
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.CategoryEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                loincCode,
                value));

        return Assert.Single(result.CancerStagings);
    }

    [Theory]
    [InlineData(NormalizationFixtures.ClinicalStageGroupCode)]
    [InlineData(NormalizationFixtures.PathologicalStageGroupCode)]
    [InlineData(NormalizationFixtures.OtherStageGroupCode)]
    public void Every_supported_stage_group_code_is_recognized(string loincCode) =>
        Assert.NotNull(StageGroupCoded(loincCode).StageGroup);

    [Theory]
    [InlineData(NormalizationFixtures.ClinicalPrimaryTumourCode)]
    [InlineData(NormalizationFixtures.PathologicalPrimaryTumourCode)]
    [InlineData(NormalizationFixtures.OtherPrimaryTumourCode)]
    public void A_primary_tumour_code_classifies_only_as_the_T_axis(string loincCode)
    {
        CancerStaging staging = MemberCoded(loincCode, StagingFixtures.PrimaryTumourValue);

        Assert.NotNull(staging.PrimaryTumour);
        Assert.Null(staging.RegionalNodes);
        Assert.Null(staging.DistantMetastases);
    }

    [Theory]
    [InlineData(NormalizationFixtures.ClinicalRegionalNodesCode)]
    [InlineData(NormalizationFixtures.PathologicalRegionalNodesCode)]
    [InlineData(NormalizationFixtures.OtherRegionalNodesCode)]
    public void A_regional_nodes_code_classifies_only_as_the_N_axis(string loincCode)
    {
        CancerStaging staging = MemberCoded(loincCode, StagingFixtures.RegionalNodesValue);

        Assert.NotNull(staging.RegionalNodes);
        Assert.Null(staging.PrimaryTumour);
        Assert.Null(staging.DistantMetastases);
    }

    [Theory]
    [InlineData(NormalizationFixtures.ClinicalDistantMetastasesCode)]
    [InlineData(NormalizationFixtures.PathologicalDistantMetastasesCode)]
    [InlineData(NormalizationFixtures.OtherDistantMetastasesCode)]
    public void A_distant_metastases_code_classifies_only_as_the_M_axis(string loincCode)
    {
        CancerStaging staging = MemberCoded(loincCode, StagingFixtures.DistantMetastasesValue);

        Assert.NotNull(staging.DistantMetastases);
        Assert.Null(staging.PrimaryTumour);
        Assert.Null(staging.RegionalNodes);
    }

    [Fact]
    public void A_member_observation_that_is_not_a_supported_category_is_ignored()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                "prognostic-factor-001",
                UnsupportedObservableLoincCode,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                NormalizationFixtures.StagingValue(
                    "http://example.org/local", "positive", null))).CancerStagings);

        Assert.Empty(staging.Categories);
        Assert.NotNull(staging.StageGroup);
    }

    [Fact]
    public void A_stage_group_code_from_another_code_system_is_not_a_stage_group()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            NormalizationFixtures.ObservationEntry(
                NormalizationFixtures.StageGroupFullUrl,
                StagingFixtures.StageGroupLogicalId,
                """ "code":{"coding":[{"system":"http://example.org/local","code":"21908-9"}]} """,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Empty(result.CancerStagings);
    }

    [Fact]
    public void Display_text_alone_never_makes_an_observation_a_stage_group()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            NormalizationFixtures.ObservationEntry(
                NormalizationFixtures.StageGroupFullUrl,
                StagingFixtures.StageGroupLogicalId,
                """
                "code":{"text":"Stage group.clinical Cancer",
                        "coding":[{"display":"Stage group.clinical Cancer"}]}
                """,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Empty(result.CancerStagings);
    }

    [Fact]
    public void Display_text_alone_never_classifies_a_member_as_a_category()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            NormalizationFixtures.ObservationEntry(
                NormalizationFixtures.PrimaryTumourFullUrl,
                StagingFixtures.PrimaryTumourLogicalId,
                """ "code":{"coding":[{"display":"Primary tumor.clinical [Class] Cancer"}]} """,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.Subject(NormalizationFixtures.PatientFullUrl),
                NormalizationFixtures.StagingValue(
                    StagingFixtures.PrimaryTumourValue))).CancerStagings);

        Assert.Empty(staging.Categories);
    }

    [Fact]
    public void A_stage_group_code_alongside_an_unrelated_coding_is_still_recognized()
    {
        NormalizationResult result = StagingFixtures.NormalizeStaging(
            NormalizationFixtures.ObservationEntry(
                NormalizationFixtures.StageGroupFullUrl,
                StagingFixtures.StageGroupLogicalId,
                """
                "code":{"coding":[{"system":"http://example.org/local","code":"STAGE"},
                                  {"system":"http://loinc.org","code":"21908-9"}]}
                """,
                NormalizationFixtures.Focus(NormalizationFixtures.ConditionFullUrl),
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue)));

        Assert.Single(result.CancerStagings);
    }

    [Fact]
    public void A_non_observation_member_reference_contributes_no_category()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(
                    NormalizationFixtures.ConditionFullUrl,
                    NormalizationFixtures.PatientFullUrl))).CancerStagings);

        Assert.Empty(staging.Categories);
    }
}
