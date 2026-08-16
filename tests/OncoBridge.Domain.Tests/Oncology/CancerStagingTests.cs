using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Domain.Tests.Oncology;

public sealed class CancerStagingTests
{
    private static readonly CodedConcept AnyStageGroup =
        new("http://cancerstaging.org", "IIA", "Stage IIA");

    private static readonly PrimaryCancerDiagnosisId AnyDiagnosis = new(Guid.NewGuid());

    private static CodedConcept Category(string code) => new("http://cancerstaging.org", code);

    private static StageCategory CategoryFor(StageAxis axis, string code) =>
        new(axis, Category(code), SourceResourceId.New());

    [Fact]
    public void Categories_arriving_from_separate_sources_become_one_addressable_assessment()
    {
        StageCategory t = CategoryFor(StageAxis.T, "T2");
        StageCategory n = CategoryFor(StageAxis.N, "N1");
        StageCategory m = CategoryFor(StageAxis.M, "M0");

        CancerStaging staging = new(
            Guid.NewGuid(),
            PatientId.New(),
            AnyDiagnosis,
            stageGroup: AnyStageGroup,
            categories: [t, n, m]);

        Assert.Equal(t, staging.PrimaryTumour);
        Assert.Equal(n, staging.RegionalNodes);
        Assert.Equal(m, staging.DistantMetastases);
        Assert.Equal(3, staging.Categories.Count);
    }

    [Fact]
    public void An_assessment_reports_every_distinct_source_its_categories_came_from()
    {
        CancerStaging staging = new(
            Guid.NewGuid(),
            PatientId.New(),
            AnyDiagnosis,
            stageGroup: AnyStageGroup,
            categories:
            [
                CategoryFor(StageAxis.T, "T2"),
                CategoryFor(StageAxis.N, "N1"),
                CategoryFor(StageAxis.M, "M0"),
            ]);

        Assert.Equal(3, staging.CategorySourceResources.Count);
    }

    [Fact]
    public void Two_categories_drawn_from_the_same_source_are_reported_once()
    {
        SourceResourceId shared = SourceResourceId.New();

        CancerStaging staging = new(
            Guid.NewGuid(),
            PatientId.New(),
            AnyDiagnosis,
            stageGroup: AnyStageGroup,
            categories:
            [
                new StageCategory(StageAxis.T, Category("T2"), shared),
                new StageCategory(StageAxis.N, Category("N1"), shared),
            ]);

        Assert.Single(staging.CategorySourceResources);
    }

    [Fact]
    public void Two_categories_on_the_same_axis_are_a_contradiction_and_are_rejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new CancerStaging(
            Guid.NewGuid(),
            PatientId.New(),
            AnyDiagnosis,
            stageGroup: AnyStageGroup,
            categories: [CategoryFor(StageAxis.T, "T2"), CategoryFor(StageAxis.T, "T3")]));

        Assert.Contains("at most one category per axis", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_assessment_asserting_neither_a_stage_group_nor_a_category_is_rejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new CancerStaging(Guid.NewGuid(), PatientId.New(), AnyDiagnosis));

        Assert.Contains("stage group or at least one category", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_stage_group_alone_is_a_valid_assessment()
    {
        CancerStaging staging = new(
            Guid.NewGuid(), PatientId.New(), AnyDiagnosis, stageGroup: AnyStageGroup);

        Assert.Empty(staging.Categories);
        Assert.Null(staging.PrimaryTumour);
    }

    [Fact]
    public void A_single_category_alone_is_a_valid_assessment()
    {
        CancerStaging staging = new(
            Guid.NewGuid(),
            PatientId.New(),
            AnyDiagnosis,
            categories: [CategoryFor(StageAxis.T, "T2")]);

        Assert.Null(staging.StageGroup);
        Assert.Single(staging.Categories);
    }

    [Fact]
    public void Null_entries_among_the_categories_are_rejected() =>
        Assert.Throws<ArgumentException>(() => new CancerStaging(
            Guid.NewGuid(),
            PatientId.New(),
            AnyDiagnosis,
            stageGroup: AnyStageGroup,
            categories: [null!]));

    [Fact]
    public void An_assessment_without_a_method_is_constructible_because_that_is_a_finding_not_an_invariant()
    {
        CancerStaging staging = new(
            Guid.NewGuid(),
            PatientId.New(),
            AnyDiagnosis,
            stageGroup: AnyStageGroup,
            method: null,
            categories: [CategoryFor(StageAxis.T, "T2")]);

        Assert.Null(staging.Method);
    }

    [Fact]
    public void A_method_is_retained_when_supplied()
    {
        StagingMethod method = new(new CodedConcept("http://snomed.info/sct", "254292007"));

        CancerStaging staging = new(
            Guid.NewGuid(), PatientId.New(), AnyDiagnosis, stageGroup: AnyStageGroup, method: method);

        Assert.Equal(method, staging.Method);
    }

    [Fact]
    public void The_category_collection_is_not_mutable_through_the_caller_supplied_list()
    {
        List<StageCategory> supplied = [CategoryFor(StageAxis.T, "T2")];

        CancerStaging staging = new(
            Guid.NewGuid(),
            PatientId.New(),
            AnyDiagnosis,
            stageGroup: AnyStageGroup,
            categories: supplied);

        supplied.Add(CategoryFor(StageAxis.N, "N1"));

        Assert.Single(staging.Categories);
    }
}
