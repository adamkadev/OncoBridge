using OncoBridge.Application.Timeline;
using OncoBridge.Domain.Temporal;
using static OncoBridge.Application.Tests.Timeline.TimelineFixtures;

namespace OncoBridge.Application.Tests.Timeline;

public sealed class TimelineGroupingTests
{
    [Fact]
    public void A_month_and_a_day_inside_it_admit_no_order()
    {
        Assert.Equal(
            TemporalComparison.Indeterminate,
            PartialDate.Compare(Month(2019, 3), Day(2019, 3, 15)));
    }

    [Fact]
    public void Anchors_that_admit_no_order_share_one_group_whose_order_is_not_established()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Month(2019, 3)))],
            stagings: [Staging(effective: Day(2019, 3, 15))]));

        TimelineGroup group = Assert.Single(timeline.Groups);

        Assert.Equal(TimelineGroupKind.OrderNotEstablished, group.Kind);
        Assert.Equal(1, group.Sequence);
        Assert.Equal(2, group.Events.Count);
        Assert.Empty(timeline.UnsequencedEvents);
    }

    [Fact]
    public void Identical_day_anchors_share_one_group_holding_the_same_temporal_anchor()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Day(2019, 4, 2)))],
            stagings: [Staging(effective: Day(2019, 4, 2))]));

        TimelineGroup group = Assert.Single(timeline.Groups);

        Assert.Equal(TimelineGroupKind.SharedTemporalAnchor, group.Kind);
        Assert.Equal(2, group.Events.Count);
    }

    [Fact]
    public void Instants_that_denote_one_moment_through_different_offsets_share_a_temporal_anchor()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Instant("2019-03-14T10:00:00+02:00")))],
            stagings: [Staging(effective: Instant("2019-03-14T08:00:00+00:00"))]));

        TimelineGroup group = Assert.Single(timeline.Groups);

        Assert.Equal(TimelineGroupKind.SharedTemporalAnchor, group.Kind);
    }

    [Fact]
    public void A_shared_temporal_anchor_leaves_every_stated_representation_untouched()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Instant("2019-03-14T10:00:00+02:00")))],
            stagings: [Staging(effective: Instant("2019-03-14T08:00:00+00:00"))]));

        Assert.Equal(
            ["2019-03-14T10:00:00+02:00", "2019-03-14T08:00:00+00:00"],
            Assert.Single(timeline.Groups).Events.Select(anchored => anchored.Anchor?.ToString()));
    }

    [Fact]
    public void The_shared_anchor_of_equivalent_instants_is_temporal_sameness_not_equality()
    {
        PartialDate stated = Instant("2019-03-14T10:00:00+02:00");
        PartialDate restated = Instant("2019-03-14T08:00:00+00:00");

        Assert.Equal(TemporalComparison.Same, PartialDate.Compare(stated, restated));
        Assert.NotEqual(stated, restated);
    }

    [Fact]
    public void A_group_holding_one_pair_that_admits_no_order_is_not_a_shared_anchor()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Day(2019, 4, 2)))],
            stagings: [Staging(effective: Day(2019, 4, 2))],
            procedures: [Procedure(performed: At(Month(2019, 4)))]));

        TimelineGroup group = Assert.Single(timeline.Groups);

        Assert.Equal(TimelineGroupKind.OrderNotEstablished, group.Kind);
        Assert.Equal(3, group.Events.Count);
    }

    [Fact]
    public void Incomparability_that_does_not_carry_across_a_cluster_still_holds_it_together()
    {
        Assert.Equal(
            TemporalComparison.Indeterminate, PartialDate.Compare(Month(2019, 3), Year(2019)));
        Assert.Equal(
            TemporalComparison.Indeterminate, PartialDate.Compare(Year(2019), Month(2019, 5)));
        Assert.Equal(
            TemporalComparison.Before, PartialDate.Compare(Month(2019, 3), Month(2019, 5)));

        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Month(2019, 3)))],
            stagings: [Staging(effective: Year(2019))],
            procedures: [Procedure(performed: At(Month(2019, 5)))]));

        TimelineGroup group = Assert.Single(timeline.Groups);

        Assert.Equal(TimelineGroupKind.OrderNotEstablished, group.Kind);
        Assert.Equal(3, group.Events.Count);
        Assert.Equal(1, group.Sequence);
    }

    [Fact]
    public void A_provable_order_inside_an_unestablished_group_is_withheld_rather_than_sequenced()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Month(2019, 5)))],
            stagings: [Staging(effective: Year(2019))],
            procedures: [Procedure(performed: At(Month(2019, 3)))]));

        TimelineGroup group = Assert.Single(timeline.Groups);

        Assert.Equal(
            ["2019-05", "2019", "2019-03"],
            group.Events.Select(anchored => anchored.Anchor?.ToString()));
    }

    [Fact]
    public void Group_sequence_follows_the_anchors_and_not_the_entity_kinds()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Day(2019, 6, 1)))],
            procedures: [Procedure(performed: At(Day(2019, 1, 5)))]));

        Assert.Equal(
            [
                TimelineEntityKind.CancerSurgicalProcedure,
                TimelineEntityKind.PrimaryCancerDiagnosis,
            ],
            timeline.Groups.Select(group => group.Events.Single().EntityKind));

        Assert.Equal([1, 2], timeline.Groups.Select(group => group.Sequence));
    }

    [Fact]
    public void An_established_group_before_and_after_an_unestablished_one_keeps_its_own_place()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses:
            [
                Diagnosis(onset: At(Month(2019, 3)), seed: 11),
                Diagnosis(onset: At(Day(2019, 3, 15)), seed: 12),
                Diagnosis(onset: At(Year(2017)), seed: 13),
            ],
            procedures: [Procedure(performed: At(Day(2021, 8, 9)))]));

        Assert.Equal(
            [
                TimelineGroupKind.Established,
                TimelineGroupKind.OrderNotEstablished,
                TimelineGroupKind.Established,
            ],
            timeline.Groups.Select(group => group.Kind));

        Assert.Equal([1, 2, 3], timeline.Groups.Select(group => group.Sequence));
        Assert.Equal([1, 2, 1], timeline.Groups.Select(group => group.Events.Count));
    }
}
