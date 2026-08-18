using OncoBridge.Application.Timeline;
using OncoBridge.Domain.Temporal;
using static OncoBridge.Application.Tests.Timeline.TimelineFixtures;

namespace OncoBridge.Application.Tests.Timeline;

public sealed class TimelineAnchorTests
{
    [Fact]
    public void A_period_with_both_bounds_is_sequenced_by_its_start_and_keeps_its_end()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            procedures: [Procedure(performed: Between(Month(2019, 5), Day(2019, 6, 12)))]));

        TimelineEvent procedure = Only(timeline);

        Assert.Empty(timeline.UnsequencedEvents);
        Assert.Equal("2019-05", procedure.Anchor?.ToString());
        Assert.Equal("2019-05", procedure.Occurrence?.Period?.Start?.ToString());
        Assert.Equal("2019-06-12", procedure.Occurrence?.Period?.End?.ToString());
    }

    [Fact]
    public void A_period_with_an_open_end_is_sequenced_by_its_start_and_states_no_end()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            procedures: [Procedure(performed: StartingAt(Month(2019, 8)))]));

        TimelineEvent procedure = Only(timeline);

        Assert.Empty(timeline.UnsequencedEvents);
        Assert.Equal("2019-08", procedure.Anchor?.ToString());
        Assert.Equal("2019-08", procedure.Occurrence?.Period?.Start?.ToString());
        Assert.Null(procedure.Occurrence?.Period?.End);
    }

    [Fact]
    public void A_period_with_no_start_is_unsequenced_and_keeps_the_end_it_states()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            procedures: [Procedure(performed: EndingAt(Day(2019, 6, 12)))]));

        UnsequencedTimelineEvent unsequenced = Assert.Single(timeline.UnsequencedEvents);

        Assert.Empty(timeline.Groups);
        Assert.Equal(UnsequencedReason.NoAnchorBound, unsequenced.Reason);
        Assert.Null(unsequenced.Event.Anchor);
        Assert.Null(unsequenced.Event.Occurrence?.Period?.Start);
        Assert.Equal("2019-06-12", unsequenced.Event.Occurrence?.Period?.End?.ToString());
        Assert.Equal(DatePrecision.Day, unsequenced.Event.Occurrence?.Period?.End?.Precision);
    }

    [Fact]
    public void A_period_end_never_stands_in_for_a_missing_start_anchor()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Day(2019, 1, 1)))],
            procedures: [Procedure(performed: EndingAt(Day(2019, 6, 12)))]));

        TimelineGroup group = Assert.Single(timeline.Groups);

        Assert.Equal(TimelineEntityKind.PrimaryCancerDiagnosis, group.Events.Single().EntityKind);
        Assert.Equal(
            TimelineEntityKind.CancerSurgicalProcedure,
            Assert.Single(timeline.UnsequencedEvents).Event.EntityKind);
    }

    [Fact]
    public void A_diagnosis_stating_no_onset_is_unsequenced_because_it_states_no_occurrence()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis()]));

        UnsequencedTimelineEvent unsequenced = Assert.Single(timeline.UnsequencedEvents);

        Assert.Empty(timeline.Groups);
        Assert.Equal(UnsequencedReason.NoOccurrenceStated, unsequenced.Reason);
        Assert.Null(unsequenced.Event.Occurrence);
        Assert.Null(unsequenced.Event.Anchor);
    }

    [Fact]
    public void A_staging_stating_no_effective_date_is_unsequenced_because_it_states_no_occurrence()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(stagings: [Staging()]));

        UnsequencedTimelineEvent unsequenced = Assert.Single(timeline.UnsequencedEvents);

        Assert.Empty(timeline.Groups);
        Assert.Equal(UnsequencedReason.NoOccurrenceStated, unsequenced.Reason);
        Assert.Null(unsequenced.Event.Occurrence);
    }

    [Fact]
    public void A_procedure_stating_no_occurrence_is_unsequenced_and_keeps_its_identity()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            procedures: [Procedure()]));

        UnsequencedTimelineEvent unsequenced = Assert.Single(timeline.UnsequencedEvents);

        Assert.Empty(timeline.Groups);
        Assert.Equal(UnsequencedReason.NoOccurrenceStated, unsequenced.Reason);
        Assert.Equal(Id(3), unsequenced.Event.EntityId);
        Assert.Equal("Lumpectomy of breast (procedure)", unsequenced.Event.Label);
    }

    [Fact]
    public void A_diagnosis_onset_stated_as_a_period_is_anchored_on_the_periods_start()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: Between(Year(2018), Month(2019, 2)))]));

        TimelineEvent diagnosis = Only(timeline);

        Assert.Equal("2018", diagnosis.Anchor?.ToString());
        Assert.Equal(DatePrecision.Year, diagnosis.Anchor?.Precision);
    }

    [Fact]
    public void An_unsequenced_event_is_never_given_a_sequence_number()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Day(2019, 1, 1)))],
            stagings: [Staging()],
            procedures: [Procedure(performed: EndingAt(Day(2019, 6, 12)))]));

        Assert.Equal([1], timeline.Groups.Select(group => group.Sequence));
        Assert.Equal(
            [UnsequencedReason.NoOccurrenceStated, UnsequencedReason.NoAnchorBound],
            timeline.UnsequencedEvents.Select(unsequenced => unsequenced.Reason));
    }
}
