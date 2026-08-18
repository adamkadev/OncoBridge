using OncoBridge.Application.Reading;
using OncoBridge.Application.Timeline;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;
using static OncoBridge.Application.Tests.Timeline.TimelineFixtures;

namespace OncoBridge.Application.Tests.Timeline;

public sealed class PatientTimelineProjectorTests
{
    private static PatientRecord AcceptanceRecord() => Record(
        diagnoses: [Diagnosis(onset: At(Month(2019, 3)), recordedDate: Day(2019, 4, 2))],
        stagings: [Staging(effective: Day(2019, 4, 2))],
        procedures: [Procedure(performed: Between(Month(2019, 5), Day(2019, 6, 12)))]);

    [Fact]
    public void The_acceptance_record_projects_three_established_groups()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(AcceptanceRecord());

        Assert.Equal([1, 2, 3], timeline.Groups.Select(group => group.Sequence));
        Assert.All(timeline.Groups, group => Assert.Equal(TimelineGroupKind.Established, group.Kind));
        Assert.All(timeline.Groups, group => Assert.Single(group.Events));
        Assert.Empty(timeline.UnsequencedEvents);
    }

    [Fact]
    public void The_acceptance_record_sequences_the_diagnosis_the_staging_then_the_procedure()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(AcceptanceRecord());

        Assert.Equal(
            [
                TimelineEntityKind.PrimaryCancerDiagnosis,
                TimelineEntityKind.CancerStaging,
                TimelineEntityKind.CancerSurgicalProcedure,
            ],
            timeline.Groups.Select(group => group.Events.Single().EntityKind));
    }

    [Fact]
    public void The_acceptance_record_anchors_each_event_on_its_stated_bound()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(AcceptanceRecord());

        Assert.Equal(
            ["2019-03", "2019-04-02", "2019-05"],
            timeline.Groups.Select(group => group.Events.Single().Anchor?.ToString()));

        Assert.Equal(
            [DatePrecision.Month, DatePrecision.Day, DatePrecision.Month],
            timeline.Groups.Select(group => group.Events.Single().Anchor?.Precision));
    }

    [Fact]
    public void The_acceptance_procedure_keeps_both_period_bounds_beside_its_start_anchor()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(AcceptanceRecord());

        TimelineEvent procedure = timeline.Groups.Single(group =>
            group.Events.Single().EntityKind == TimelineEntityKind.CancerSurgicalProcedure).Events.Single();

        PartialPeriod period = Assert.IsType<PartialPeriod>(procedure.Occurrence?.Period);

        Assert.Equal("2019-05", period.Start?.ToString());
        Assert.Equal("2019-06-12", period.End?.ToString());
        Assert.Equal("2019-05", procedure.Anchor?.ToString());
    }

    [Fact]
    public void The_acceptance_record_carries_the_labels_the_canonical_codes_state()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(AcceptanceRecord());

        Assert.Equal(
            [
                "Malignant neoplasm of breast (disorder)",
                "Stage IIA",
                "Lumpectomy of breast (procedure)",
            ],
            timeline.Groups.Select(group => group.Events.Single().Label));
    }

    [Fact]
    public void Every_response_states_the_projection_policy()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(AcceptanceRecord());

        Assert.Equal("1.0.0", timeline.ProjectionPolicy.Version);
        Assert.Equal(
            "Events are sequenced by their temporal anchor, projected on stated bounds only. "
                + "A period is anchored by its stated start bound.",
            timeline.ProjectionPolicy.Description);
    }

    [Fact]
    public void The_projected_patient_is_the_patient_of_the_record()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(AcceptanceRecord());

        Assert.Equal(Subject, timeline.PatientId);
    }

    [Fact]
    public void A_record_with_no_oncology_entities_projects_nothing()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record());

        Assert.Empty(timeline.Groups);
        Assert.Empty(timeline.UnsequencedEvents);
    }

    [Fact]
    public void A_birth_date_is_not_a_timeline_event()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(birthDate: Year(1968)));

        Assert.Empty(timeline.Groups);
        Assert.Empty(timeline.UnsequencedEvents);
    }

    [Fact]
    public void A_recorded_date_does_not_become_a_second_event()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(onset: At(Month(2019, 3)), recordedDate: Day(2019, 4, 2))]));

        TimelineEvent diagnosis = Only(timeline);

        Assert.Empty(timeline.UnsequencedEvents);
        Assert.Equal("2019-03", diagnosis.Anchor?.ToString());
        Assert.Equal("2019-04-02", diagnosis.Diagnosis?.RecordedDate?.ToString());
    }

    [Fact]
    public void A_recorded_date_never_anchors_a_diagnosis_that_states_no_onset()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses: [Diagnosis(recordedDate: Day(2019, 4, 2))]));

        UnsequencedTimelineEvent unsequenced = Assert.Single(timeline.UnsequencedEvents);

        Assert.Empty(timeline.Groups);
        Assert.Equal(UnsequencedReason.NoOccurrenceStated, unsequenced.Reason);
        Assert.Null(unsequenced.Event.Anchor);
        Assert.Equal("2019-04-02", unsequenced.Event.Diagnosis?.RecordedDate?.ToString());
    }

    [Fact]
    public void A_staging_effective_date_is_projected_as_a_stated_date_occurrence()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            stagings: [Staging(effective: Day(2019, 4, 2))]));

        TimelineEvent staging = Only(timeline);

        Assert.Equal(TemporalOccurrenceKind.Date, staging.Occurrence?.Kind);
        Assert.Equal("2019-04-02", staging.Occurrence?.Date?.ToString());
        Assert.Equal("2019-04-02", staging.Anchor?.ToString());
    }

    [Fact]
    public void A_staging_carries_its_stage_group_and_axis_ordered_categories()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            stagings:
            [
                Staging(
                    effective: Day(2019, 4, 2),
                    stageGroup: StageIIA,
                    categories:
                    [
                        Category(StageAxis.M, "M0"),
                        Category(StageAxis.T, "T2"),
                        Category(StageAxis.N, "N1"),
                    ]),
            ]));

        TimelineStagingDetail staging = Assert.IsType<TimelineStagingDetail>(Only(timeline).Staging);

        Assert.Equal("IIA", staging.StageGroup?.Code);
        Assert.Equal(
            [StageAxis.T, StageAxis.N, StageAxis.M],
            staging.Categories.Select(category => category.Axis));
    }

    [Fact]
    public void A_diagnosis_label_falls_back_to_its_code_when_no_display_is_stated()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            diagnoses:
            [
                Diagnosis(
                    onset: At(Month(2019, 3)),
                    code: new CodedConcept("http://snomed.info/sct", "254837009")),
            ]));

        Assert.Equal("254837009", Only(timeline).Label);
    }

    [Fact]
    public void A_procedure_label_falls_back_to_its_code_when_no_display_is_stated()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            procedures:
            [
                Procedure(
                    performed: At(Day(2019, 5, 20)),
                    code: new CodedConcept("http://snomed.info/sct", "392021009")),
            ]));

        Assert.Equal("392021009", Only(timeline).Label);
    }

    [Fact]
    public void A_staging_label_falls_back_to_its_stage_group_code_when_no_display_is_stated()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            stagings:
            [
                Staging(
                    effective: Day(2019, 4, 2),
                    stageGroup: new CodedConcept("http://cancerstaging.org", "IIA")),
            ]));

        Assert.Equal("IIA", Only(timeline).Label);
    }

    [Fact]
    public void A_staging_with_no_stage_group_is_labelled_by_its_axis_ordered_categories()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Record(
            stagings:
            [
                Staging(
                    effective: Day(2019, 4, 2),
                    categories:
                    [
                        Category(StageAxis.N, "N1"),
                        Category(StageAxis.M, "M0"),
                        Category(StageAxis.T, "T2"),
                    ]),
            ]));

        Assert.Equal("T2 N1 M0", Only(timeline).Label);
    }

    [Fact]
    public void A_timeline_event_exposes_no_sequence_of_its_own()
    {
        string[] offenders =
        [
            .. typeof(TimelineEvent).GetProperties()
                .Select(property => property.Name)
                .Where(name => name.Contains("Sequence", StringComparison.Ordinal)),
        ];

        Assert.True(
            offenders.Length == 0,
            "A sequence belongs to a group, never to an event inside one, but TimelineEvent exposes: "
                + $"{string.Join(", ", offenders)}.");
    }
}
