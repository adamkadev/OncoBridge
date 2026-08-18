using OncoBridge.Api.Contracts;
using OncoBridge.Application.Timeline;

namespace OncoBridge.Api.Mapping;

internal static class PatientTimelineMapping
{
    internal static PatientTimelineResponse ToResponse(PatientTimeline timeline) => new()
    {
        PatientId = timeline.PatientId.Value,
        ProjectionPolicy = ToResponse(timeline.ProjectionPolicy),
        Groups = [.. timeline.Groups.Select(ToResponse)],
        UnsequencedEvents = [.. timeline.UnsequencedEvents.Select(ToResponse)],
    };

    private static TimelineProjectionPolicyResponse ToResponse(TimelineProjectionPolicy policy) => new()
    {
        Version = policy.Version,
        Description = policy.Description,
    };

    private static TimelineGroupResponse ToResponse(TimelineGroup group) => new()
    {
        Sequence = group.Sequence,
        Kind = group.Kind.ToString(),
        Events = [.. group.Events.Select(ToResponse)],
    };

    private static UnsequencedTimelineEventResponse ToResponse(UnsequencedTimelineEvent unsequenced) => new()
    {
        Reason = unsequenced.Reason.ToString(),
        Event = ToResponse(unsequenced.Event),
    };

    private static TimelineEventResponse ToResponse(TimelineEvent timelineEvent) => new()
    {
        EntityId = timelineEvent.EntityId,
        EntityKind = timelineEvent.EntityKind.ToString(),
        Label = timelineEvent.Label,
        Anchor = CanonicalValueMapping.ToResponseOrNull(timelineEvent.Anchor),
        Occurrence = CanonicalValueMapping.ToResponseOrNull(timelineEvent.Occurrence),
        Diagnosis = timelineEvent.Diagnosis is { } diagnosis ? ToResponse(diagnosis) : null,
        Staging = timelineEvent.Staging is { } staging ? ToResponse(staging) : null,
        Procedure = timelineEvent.Procedure is { } procedure ? ToResponse(procedure) : null,
    };

    private static TimelineDiagnosisDetailResponse ToResponse(TimelineDiagnosisDetail diagnosis) => new()
    {
        Code = CanonicalValueMapping.ToResponse(diagnosis.Code),
        RecordedDate = CanonicalValueMapping.ToResponseOrNull(diagnosis.RecordedDate),
    };

    private static TimelineStagingDetailResponse ToResponse(TimelineStagingDetail staging) => new()
    {
        StageGroup = CanonicalValueMapping.ToResponseOrNull(staging.StageGroup),
        Categories = [.. staging.Categories.Select(CanonicalValueMapping.ToResponse)],
    };

    private static TimelineProcedureDetailResponse ToResponse(TimelineProcedureDetail procedure) => new()
    {
        Code = CanonicalValueMapping.ToResponse(procedure.Code),
    };
}
