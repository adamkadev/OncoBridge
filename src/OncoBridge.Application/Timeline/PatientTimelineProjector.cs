using OncoBridge.Application.Reading;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Application.Timeline;

public static class PatientTimelineProjector
{
    private sealed record Anchored(TimelineEvent Event, PartialDate Anchor);

    private sealed record Anchoring(PartialDate? Anchor, TimelineAnchorSource? Source);

    public static PatientTimeline Project(PatientRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        List<Anchored> anchored = [];
        List<UnsequencedTimelineEvent> unsequenced = [];

        foreach (TimelineEvent candidate in TechnicalOrder(Extract(record)))
        {
            switch (candidate)
            {
                case { Occurrence: null }:
                    unsequenced.Add(Unsequenced(candidate, UnsequencedReason.NoOccurrenceStated));
                    break;

                case { Anchor: null }:
                    unsequenced.Add(Unsequenced(candidate, UnsequencedReason.NoAnchorBound));
                    break;

                case { Anchor: { } anchor }:
                    anchored.Add(new Anchored(candidate, anchor));
                    break;
            }
        }

        return new PatientTimeline
        {
            PatientId = record.Patient.Id,
            ProjectionPolicy = TimelineProjectionPolicy.V1,
            Groups = GroupsOf(anchored),
            UnsequencedEvents = unsequenced,
        };
    }

    private static IEnumerable<TimelineEvent> TechnicalOrder(IEnumerable<TimelineEvent> events) =>
        events.OrderBy(candidate => candidate.EntityKind).ThenBy(candidate => candidate.EntityId);

    private static UnsequencedTimelineEvent Unsequenced(TimelineEvent candidate, UnsequencedReason reason) =>
        new() { Reason = reason, Event = candidate };

    private static IReadOnlyList<TimelineGroup> GroupsOf(List<Anchored> anchored)
    {
        if (anchored.Count == 0)
        {
            return [];
        }

        int[] roots = ConnectedComponents(anchored);

        List<List<int>> components =
        [
            .. roots
                .Select((root, index) => (Root: root, Index: index))
                .GroupBy(member => member.Root)
                .Select(component => component.Select(member => member.Index).ToList()),
        ];

        int[] sequences = SequencesOf(components, anchored);

        return
        [
            .. components
                .Select((members, component) => new TimelineGroup
                {
                    Sequence = sequences[component],
                    Kind = KindOf(members, anchored),
                    Events = [.. members.Select(member => anchored[member].Event)],
                })
                .OrderBy(group => group.Sequence),
        ];
    }

    private static int[] ConnectedComponents(List<Anchored> anchored)
    {
        int[] parent = [.. Enumerable.Range(0, anchored.Count)];

        foreach ((int left, int right) in Pairs(anchored.Count))
        {
            if (Compare(anchored, left, right)
                is TemporalComparison.Same or TemporalComparison.Indeterminate)
            {
                Union(parent, left, right);
            }
        }

        return [.. Enumerable.Range(0, anchored.Count).Select(index => Find(parent, index))];
    }

    private static TimelineGroupKind KindOf(List<int> members, List<Anchored> anchored)
    {
        if (members.Count == 1)
        {
            return TimelineGroupKind.Established;
        }

        bool sharedAnchor = Pairs(members)
            .All(pair => Compare(anchored, pair.Left, pair.Right) == TemporalComparison.Same);

        return sharedAnchor
            ? TimelineGroupKind.SharedTemporalAnchor
            : TimelineGroupKind.OrderNotEstablished;
    }

    private static int[] SequencesOf(List<List<int>> components, List<Anchored> anchored)
    {
        int[] earlier = new int[components.Count];

        foreach ((int left, int right) in Pairs(components.Count))
        {
            if (DirectionBetween(components[left], components[right], anchored)
                == TemporalComparison.After)
            {
                earlier[left]++;
            }
            else
            {
                earlier[right]++;
            }
        }

        if (earlier.Distinct().Count() != earlier.Length)
        {
            throw new InvalidOperationException(
                "Timeline components must rank uniquely under the verified strict order, but "
                    + $"ranked [{string.Join(", ", earlier)}]. A projection may not invent a sequence.");
        }

        return [.. earlier.Select(count => count + 1)];
    }

    private static TemporalComparison DirectionBetween(
        List<int> left, List<int> right, List<Anchored> anchored)
    {
        TemporalComparison direction = Compare(anchored, left[0], right[0]);

        foreach (int leftMember in left)
        {
            foreach (int rightMember in right)
            {
                TemporalComparison comparison = Compare(anchored, leftMember, rightMember);

                if (comparison != direction
                    || comparison is not (TemporalComparison.Before or TemporalComparison.After))
                {
                    throw new InvalidOperationException(
                        "Timeline components must be strictly ordered, but anchor "
                            + $"'{anchored[leftMember].Anchor}' compares '{comparison}' to "
                            + $"'{anchored[rightMember].Anchor}' where '{direction}' holds elsewhere in "
                            + "the same component pair. A projection may not choose a direction.");
                }
            }
        }

        return direction;
    }

    private static TemporalComparison Compare(List<Anchored> anchored, int left, int right) =>
        PartialDate.Compare(anchored[left].Anchor, anchored[right].Anchor);

    private static IEnumerable<(int Left, int Right)> Pairs(int count) =>
        Pairs([.. Enumerable.Range(0, count)]);

    private static IEnumerable<(int Left, int Right)> Pairs(IReadOnlyList<int> members)
    {
        for (int left = 0; left < members.Count; left++)
        {
            for (int right = left + 1; right < members.Count; right++)
            {
                yield return (members[left], members[right]);
            }
        }
    }

    private static int Find(int[] parent, int index)
    {
        while (parent[index] != index)
        {
            parent[index] = parent[parent[index]];
            index = parent[index];
        }

        return index;
    }

    private static void Union(int[] parent, int left, int right)
    {
        int leftRoot = Find(parent, left);
        int rightRoot = Find(parent, right);

        if (leftRoot != rightRoot)
        {
            parent[Math.Max(leftRoot, rightRoot)] = Math.Min(leftRoot, rightRoot);
        }
    }

    private static IEnumerable<TimelineEvent> Extract(PatientRecord record)
    {
        foreach (PrimaryCancerDiagnosis diagnosis in record.PrimaryCancerDiagnoses)
        {
            Anchoring onset = AnchoringOf(diagnosis.Onset);

            yield return new TimelineEvent
            {
                EntityId = diagnosis.Id.Value,
                EntityKind = TimelineEntityKind.PrimaryCancerDiagnosis,
                Label = LabelOf(diagnosis.Code),
                Anchor = onset.Anchor,
                AnchorSource = onset.Source,
                Occurrence = diagnosis.Onset,
                Diagnosis = new TimelineDiagnosisDetail
                {
                    Code = diagnosis.Code,
                    RecordedDate = diagnosis.RecordedDate,
                },
            };
        }

        foreach (CancerStaging staging in record.CancerStagings)
        {
            TemporalOccurrence? effective =
                staging.Effective is { } date ? TemporalOccurrence.FromDate(date) : null;

            Anchoring assessed = AnchoringOf(effective);

            yield return new TimelineEvent
            {
                EntityId = staging.Id,
                EntityKind = TimelineEntityKind.CancerStaging,
                Label = LabelOf(staging),
                Anchor = assessed.Anchor,
                AnchorSource = assessed.Source,
                Occurrence = effective,
                Staging = new TimelineStagingDetail
                {
                    StageGroup = staging.StageGroup,
                    Categories = AxisOrder(staging),
                },
            };
        }

        foreach (CancerSurgicalProcedure procedure in record.CancerSurgicalProcedures)
        {
            Anchoring performed = AnchoringOf(procedure.Performed);

            yield return new TimelineEvent
            {
                EntityId = procedure.Id,
                EntityKind = TimelineEntityKind.CancerSurgicalProcedure,
                Label = LabelOf(procedure.Code),
                Anchor = performed.Anchor,
                AnchorSource = performed.Source,
                Occurrence = procedure.Performed,
                Procedure = new TimelineProcedureDetail { Code = procedure.Code },
            };
        }
    }

    private static Anchoring AnchoringOf(TemporalOccurrence? occurrence) => occurrence switch
    {
        { Date: { } date } => new Anchoring(date, TimelineAnchorSource.Date),
        { Period.Start: { } start } => new Anchoring(start, TimelineAnchorSource.PeriodStart),
        _ => new Anchoring(null, null),
    };

    private static IReadOnlyList<StageCategory> AxisOrder(CancerStaging staging) =>
        [.. staging.Categories.OrderBy(category => category.Axis)];

    private static string LabelOf(CancerStaging staging) =>
        staging.StageGroup is { } stageGroup
            ? LabelOf(stageGroup)
            : string.Join(" ", AxisOrder(staging).Select(category => LabelOf(category.Code)));

    private static string LabelOf(CodedConcept concept) => concept.Display ?? concept.Code;
}
