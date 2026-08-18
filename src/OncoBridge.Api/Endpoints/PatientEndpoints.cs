using Microsoft.AspNetCore.Mvc;
using OncoBridge.Api.Contracts;
using OncoBridge.Api.Mapping;
using OncoBridge.Application.Reading;
using OncoBridge.Application.Timeline;
using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Api.Endpoints;

internal static class PatientEndpoints
{
    private const string PatientsTag = "Patients";

    internal static RouteGroupBuilder MapPatientEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/patients/{patientId:guid}/record", GetPatientRecordAsync)
            .WithName("GetPatientRecord")
            .WithTags(PatientsTag)
            .WithSummary("Read the canonical record of a patient")
            .WithDescription(
                "Returns the canonical patient together with the primary cancer diagnoses, TNM "
                + "staging assessments and cancer-related surgical procedures derived for them. "
                + "Variable-precision dates keep the precision the source stated.")
            .Produces<PatientRecordResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/patients/{patientId:guid}/timeline", GetPatientTimelineAsync)
            .WithName("GetPatientTimeline")
            .WithTags(PatientsTag)
            .WithSummary("Read the projected longitudinal timeline of a patient")
            .WithDescription(
                "Projects the canonical record onto a sequence of temporal groups. Events are "
                + "sequenced by their temporal anchor, projected on stated bounds only, and a "
                + "period is anchored by its stated start bound. Anchors that compare as the same "
                + "instant, or whose stated precision admits no ordering, share one group rather "
                + "than being given an order the record does not state. An event with no usable "
                + "anchor is returned unsequenced, with the reason and every bound it did state.")
            .Produces<PatientTimelineResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetPatientRecordAsync(
        Guid patientId,
        [FromServices] GetPatientRecord getPatientRecord,
        CancellationToken cancellationToken)
    {
        PatientRecord? record =
            await getPatientRecord.ExecuteAsync(new PatientId(patientId), cancellationToken);

        return record is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(PatientRecordMapping.ToResponse(record));
    }

    private static async Task<IResult> GetPatientTimelineAsync(
        Guid patientId,
        [FromServices] GetPatientTimeline getPatientTimeline,
        CancellationToken cancellationToken)
    {
        PatientTimeline? timeline =
            await getPatientTimeline.ExecuteAsync(new PatientId(patientId), cancellationToken);

        return timeline is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(PatientTimelineMapping.ToResponse(timeline));
    }
}
