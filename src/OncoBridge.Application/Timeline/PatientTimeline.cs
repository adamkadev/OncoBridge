using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Temporal;
using OncoBridge.Domain.Terminology;

namespace OncoBridge.Application.Timeline;

public enum TimelineEntityKind
{
    PrimaryCancerDiagnosis,

    CancerStaging,

    CancerSurgicalProcedure,
}

public enum TimelineGroupKind
{
    Established,

    SharedTemporalAnchor,

    OrderNotEstablished,
}

public enum TimelineAnchorSource
{
    Date,

    PeriodStart,
}

public enum UnsequencedReason
{
    NoOccurrenceStated,

    NoAnchorBound,
}

public sealed record TimelineProjectionPolicy
{
    public required string Version { get; init; }

    public required string Description { get; init; }

    public static TimelineProjectionPolicy V1 { get; } = new()
    {
        Version = "1.0.0",
        Description =
            "Events are sequenced by their temporal anchor, projected on stated bounds only. "
            + "A period is anchored by its stated start bound.",
    };
}

public sealed record TimelineDiagnosisDetail
{
    public required CodedConcept Code { get; init; }

    public PartialDate? RecordedDate { get; init; }
}

public sealed record TimelineStagingDetail
{
    public CodedConcept? StageGroup { get; init; }

    public required IReadOnlyList<StageCategory> Categories { get; init; }
}

public sealed record TimelineProcedureDetail
{
    public required CodedConcept Code { get; init; }
}

public sealed record TimelineEvent
{
    public required Guid EntityId { get; init; }

    public required TimelineEntityKind EntityKind { get; init; }

    public required string Label { get; init; }

    public PartialDate? Anchor { get; init; }

    public TimelineAnchorSource? AnchorSource { get; init; }

    public TemporalOccurrence? Occurrence { get; init; }

    public TimelineDiagnosisDetail? Diagnosis { get; init; }

    public TimelineStagingDetail? Staging { get; init; }

    public TimelineProcedureDetail? Procedure { get; init; }
}

public sealed record TimelineGroup
{
    public required int Sequence { get; init; }

    public required TimelineGroupKind Kind { get; init; }

    public required IReadOnlyList<TimelineEvent> Events { get; init; }
}

public sealed record UnsequencedTimelineEvent
{
    public required UnsequencedReason Reason { get; init; }

    public required TimelineEvent Event { get; init; }
}

public sealed record PatientTimeline
{
    public required PatientId PatientId { get; init; }

    public required TimelineProjectionPolicy ProjectionPolicy { get; init; }

    public required IReadOnlyList<TimelineGroup> Groups { get; init; }

    public required IReadOnlyList<UnsequencedTimelineEvent> UnsequencedEvents { get; init; }
}
