using OncoBridge.Application.Reading;
using OncoBridge.Application.Timeline;
using OncoBridge.Domain.Oncology;
using static OncoBridge.Application.Tests.Timeline.TimelineFixtures;

namespace OncoBridge.Application.Tests.Timeline;

public sealed class TimelineDeterminismTests
{
    private static readonly PrimaryCancerDiagnosis[] Diagnoses =
    [
        Diagnosis(onset: At(Day(2019, 4, 2)), seed: 12),
        Diagnosis(onset: At(Day(2019, 4, 2)), seed: 11),
    ];

    private static readonly CancerStaging[] Stagings =
    [
        Staging(effective: Day(2020, 2, 2), seed: 31),
        Staging(seed: 32),
    ];

    private static readonly CancerSurgicalProcedure[] Procedures =
    [
        Procedure(performed: At(Day(2019, 9, 1)), seed: 21),
        Procedure(performed: EndingAt(Day(2019, 6, 12)), seed: 22),
    ];

    private static PatientRecord AsGiven() => Record(Diagnoses, Stagings, Procedures);

    private static PatientRecord Reversed() => Record(
        Diagnoses.Reverse(), Stagings.Reverse(), Procedures.Reverse());

    [Fact]
    public void Reordering_the_canonical_collections_does_not_change_the_projection() =>
        Assert.Equal(
            Describe(PatientTimelineProjector.Project(AsGiven())),
            Describe(PatientTimelineProjector.Project(Reversed())));

    [Fact]
    public void Every_permutation_of_the_diagnoses_projects_the_same_timeline()
    {
        string expected = Describe(PatientTimelineProjector.Project(AsGiven()));

        Assert.All(
            Permutations(Diagnoses),
            permutation => Assert.Equal(
                expected,
                Describe(PatientTimelineProjector.Project(
                    Record(permutation, Stagings, Procedures)))));
    }

    [Fact]
    public void Events_sharing_an_anchor_serialize_by_entity_kind_then_entity_id()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Reversed());

        TimelineGroup shared = timeline.Groups.Single(group =>
            group.Kind == TimelineGroupKind.SharedTemporalAnchor);

        Assert.Equal([Id(11), Id(12)], shared.Events.Select(anchored => anchored.EntityId));
    }

    [Fact]
    public void The_unsequenced_events_serialize_by_entity_kind_then_entity_id()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Reversed());

        Assert.Equal(
            [
                TimelineEntityKind.CancerStaging,
                TimelineEntityKind.CancerSurgicalProcedure,
            ],
            timeline.UnsequencedEvents.Select(unsequenced => unsequenced.Event.EntityKind));
    }

    [Fact]
    public void The_group_sequence_stays_temporal_while_the_input_order_varies()
    {
        PatientTimeline timeline = PatientTimelineProjector.Project(Reversed());

        Assert.Equal([1, 2, 3], timeline.Groups.Select(group => group.Sequence));
        Assert.Equal(
            [
                TimelineGroupKind.SharedTemporalAnchor,
                TimelineGroupKind.Established,
                TimelineGroupKind.Established,
            ],
            timeline.Groups.Select(group => group.Kind));
        Assert.Equal(
            ["2019-04-02", "2019-09-01", "2020-02-02"],
            timeline.Groups.Select(group => group.Events[0].Anchor?.ToString()));
    }

    private static IEnumerable<T[]> Permutations<T>(IReadOnlyList<T> items)
    {
        if (items.Count <= 1)
        {
            yield return [.. items];
            yield break;
        }

        for (int index = 0; index < items.Count; index++)
        {
            T head = items[index];
            T[] rest = [.. items.Where((_, position) => position != index)];

            foreach (T[] tail in Permutations(rest))
            {
                yield return [head, .. tail];
            }
        }
    }

    private static string Describe(PatientTimeline timeline) =>
        string.Join(
            "\n",
            timeline.Groups
                .Select(group =>
                    $"{group.Sequence} {group.Kind} "
                        + string.Join(", ", group.Events.Select(Describe)))
                .Concat(timeline.UnsequencedEvents.Select(unsequenced =>
                    $"- {unsequenced.Reason} {Describe(unsequenced.Event)}")));

    private static string Describe(TimelineEvent candidate) =>
        $"{candidate.EntityKind}:{candidate.EntityId}@{candidate.Anchor?.ToString() ?? "unanchored"}";
}
