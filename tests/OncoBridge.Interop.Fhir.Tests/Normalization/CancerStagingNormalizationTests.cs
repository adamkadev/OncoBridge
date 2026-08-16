using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;
using OncoBridge.Interop.Fhir.Ingestion;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

public sealed class CancerStagingNormalizationTests
{
    private static CancerStaging FixtureStaging() =>
        Assert.Single(NormalizationFixtures.NormalizeTnmStagingBundle().CancerStagings);

    [Fact]
    public void A_stage_group_observation_and_its_members_become_one_staging_assessment()
    {
        NormalizationResult result = NormalizationFixtures.NormalizeTnmStagingBundle();

        CancerStaging staging = Assert.Single(result.CancerStagings);

        Assert.Equal(Assert.Single(result.Patients).Id, staging.PatientId);
        Assert.Equal(3, staging.Categories.Count);
    }

    [Fact]
    public void The_stage_group_value_preserves_system_code_and_display_exactly() =>
        Assert.Equal(
            new CodedConcept("http://cancerstaging.org", "IIA", "Stage IIA"), FixtureStaging().StageGroup);

    [Fact]
    public void The_staging_method_is_read_from_the_observation_method_and_not_inferred() =>
        Assert.Equal(
            new CodedConcept("http://snomed.info/sct", "254292007"), FixtureStaging().Method!.Code);

    [Fact]
    public void The_effective_date_keeps_the_precision_the_source_stated()
    {
        CancerStaging staging = FixtureStaging();

        Assert.Equal(PartialDate.FromDate(2019, 4, 2), staging.Effective);
        Assert.Equal(DatePrecision.Day, staging.Effective!.Precision);
    }

    [Fact]
    public void Each_axis_maps_to_the_category_its_own_member_observation_reported()
    {
        CancerStaging staging = FixtureStaging();

        Assert.Equal(new CodedConcept("http://cancerstaging.org", "T2"), staging.PrimaryTumour!.Code);
        Assert.Equal(new CodedConcept("http://cancerstaging.org", "N1"), staging.RegionalNodes!.Code);
        Assert.Equal(
            new CodedConcept("http://cancerstaging.org", "M0"), staging.DistantMetastases!.Code);
    }

    [Fact]
    public void Every_category_names_the_member_observation_it_came_from()
    {
        IngestedBundle ingested = NormalizationFixtures.IngestTnmStagingBundle();

        CancerStaging staging =
            Assert.Single(NormalizationFixtures.Normalize(ingested.SourceResources).CancerStagings);

        Assert.Equal(3, staging.CategorySourceResources.Count);
        Assert.Equal(
            ingested.SourceResources.Single(source => source.SourceLogicalId == "staging-t-001").Id,
            staging.PrimaryTumour!.SourceResourceId);
    }

    [Fact]
    public void A_year_precision_effective_date_is_not_widened_to_a_full_date()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.EffectiveDateTime("2019"))).CancerStagings);

        Assert.Equal(PartialDate.FromYear(2019), staging.Effective);
        Assert.Null(staging.Effective!.Month);
    }

    [Fact]
    public void A_month_precision_effective_date_stays_at_month_precision()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.EffectiveDateTime("2019-04"))).CancerStagings);

        Assert.Equal(PartialDate.FromYearMonth(2019, 4), staging.Effective);
        Assert.Null(staging.Effective!.Day);
    }

    [Fact]
    public void A_full_effective_time_stamp_keeps_its_instant_and_stated_offset()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.EffectiveDateTime("2019-04-02T09:30:00+02:00"))).CancerStagings);

        Assert.Equal(DatePrecision.Instant, staging.Effective!.Precision);
        Assert.Equal(TimeSpan.FromHours(2), staging.Effective.Instant!.Value.Offset);
    }

    [Fact]
    public void An_effective_instant_maps_at_instant_precision()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                """ "effectiveInstant":"2019-04-02T07:30:00Z" """)).CancerStagings);

        Assert.Equal(DatePrecision.Instant, staging.Effective!.Precision);
        Assert.Equal(
            new DateTimeOffset(2019, 4, 2, 7, 30, 0, TimeSpan.Zero), staging.Effective.Instant);
    }

    [Fact]
    public void An_effective_period_is_not_collapsed_into_its_start()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                """ "effectivePeriod":{"start":"2019-04-02","end":"2019-04-09"} """)).CancerStagings);

        Assert.Null(staging.Effective);
    }

    [Fact]
    public void An_absent_method_leaves_the_assessment_without_one_rather_than_rejecting_it()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue))).CancerStagings);

        Assert.Null(staging.Method);
        Assert.NotNull(staging.StageGroup);
    }

    [Fact]
    public void A_method_whose_codings_lack_a_system_or_a_code_is_not_mapped()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                """ "method":{"coding":[{"display":"Nothing usable"}]} """)).CancerStagings);

        Assert.Null(staging.Method);
    }

    [Fact]
    public void The_method_is_never_inferred_from_the_stage_group_observation_code()
    {
        CancerStaging staging = Assert.Single(StagingFixtures.NormalizeStaging(
            StagingFixtures.LinkedStageGroupEntry(
                NormalizationFixtures.StagingValue(StagingFixtures.StageGroupValue),
                NormalizationFixtures.HasMember(NormalizationFixtures.PrimaryTumourFullUrl)),
            StagingFixtures.PrimaryTumourEntry()).CancerStagings);

        Assert.Null(staging.Method);
    }
}
