using Microsoft.AspNetCore.Mvc;
using OncoBridge.Api.Contracts;
using OncoBridge.Api.Mapping;
using OncoBridge.Application.Reading;
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
}
