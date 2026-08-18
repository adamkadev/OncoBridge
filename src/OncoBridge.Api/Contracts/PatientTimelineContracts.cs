namespace OncoBridge.Api.Contracts;

public sealed record TimelineProjectionPolicyResponse
{
    public required string Version { get; init; }

    public required string Description { get; init; }
}

public sealed record TimelineDiagnosisDetailResponse
{
    public required CodedConceptResponse Code { get; init; }

    public PartialDateResponse? RecordedDate { get; init; }
}

public sealed record TimelineStagingDetailResponse
{
    public CodedConceptResponse? StageGroup { get; init; }

    public required IReadOnlyList<StageCategoryResponse> Categories { get; init; }
}

public sealed record TimelineProcedureDetailResponse
{
    public required CodedConceptResponse Code { get; init; }
}

public sealed record TimelineEventResponse
{
    public required Guid EntityId { get; init; }

    public required string EntityKind { get; init; }

    public required string Label { get; init; }

    public PartialDateResponse? Anchor { get; init; }

    public TemporalOccurrenceResponse? Occurrence { get; init; }

    public TimelineDiagnosisDetailResponse? Diagnosis { get; init; }

    public TimelineStagingDetailResponse? Staging { get; init; }

    public TimelineProcedureDetailResponse? Procedure { get; init; }
}

public sealed record TimelineGroupResponse
{
    public required int Sequence { get; init; }

    public required string Kind { get; init; }

    public required IReadOnlyList<TimelineEventResponse> Events { get; init; }
}

public sealed record UnsequencedTimelineEventResponse
{
    public required string Reason { get; init; }

    public required TimelineEventResponse Event { get; init; }
}

public sealed record PatientTimelineResponse
{
    public required Guid PatientId { get; init; }

    public required TimelineProjectionPolicyResponse ProjectionPolicy { get; init; }

    public required IReadOnlyList<TimelineGroupResponse> Groups { get; init; }

    public required IReadOnlyList<UnsequencedTimelineEventResponse> UnsequencedEvents { get; init; }
}
