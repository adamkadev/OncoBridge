using OncoBridge.Api.Contracts;
using OncoBridge.Application.Reading;
using OncoBridge.Domain.Oncology;

namespace OncoBridge.Api.Mapping;

internal static class PatientRecordMapping
{
    internal static PatientRecordResponse ToResponse(PatientRecord record) => new()
    {
        Patient = ToResponse(record.Patient),
        PrimaryCancerDiagnoses = [.. record.PrimaryCancerDiagnoses.Select(ToResponse)],
        CancerStagings = [.. record.CancerStagings.Select(ToResponse)],
        CancerSurgicalProcedures = [.. record.CancerSurgicalProcedures.Select(ToResponse)],
    };

    private static PatientResponse ToResponse(Patient patient) => new()
    {
        Id = patient.Id.Value,
        SourceIdentifier = patient.SourceIdentifier,
        BirthDate = CanonicalValueMapping.ToResponseOrNull(patient.BirthDate),
        SexAtBirthAsRecorded = CanonicalValueMapping.ToResponseOrNull(patient.SexAtBirthAsRecorded),
    };

    private static PrimaryCancerDiagnosisResponse ToResponse(PrimaryCancerDiagnosis diagnosis) => new()
    {
        Id = diagnosis.Id.Value,
        PatientId = diagnosis.PatientId.Value,
        Code = CanonicalValueMapping.ToResponse(diagnosis.Code),
        Onset = CanonicalValueMapping.ToResponseOrNull(diagnosis.Onset),
        BodySite = CanonicalValueMapping.ToResponseOrNull(diagnosis.BodySite),
        RecordedDate = CanonicalValueMapping.ToResponseOrNull(diagnosis.RecordedDate),
    };

    private static CancerStagingResponse ToResponse(CancerStaging staging) => new()
    {
        Id = staging.Id,
        PatientId = staging.PatientId.Value,
        PrimaryCancerDiagnosisId = staging.PrimaryCancerDiagnosisId.Value,
        StageGroup = CanonicalValueMapping.ToResponseOrNull(staging.StageGroup),
        Method = CanonicalValueMapping.ToResponseOrNull(staging.Method?.Code),
        Effective = CanonicalValueMapping.ToResponseOrNull(staging.Effective),
        Categories =
        [
            .. staging.Categories
                .OrderBy(category => category.Axis)
                .Select(CanonicalValueMapping.ToResponse),
        ],
    };

    private static CancerSurgicalProcedureResponse ToResponse(CancerSurgicalProcedure procedure) => new()
    {
        Id = procedure.Id,
        PatientId = procedure.PatientId.Value,
        Code = CanonicalValueMapping.ToResponse(procedure.Code),
        Performed = CanonicalValueMapping.ToResponseOrNull(procedure.Performed),
        BodySite = CanonicalValueMapping.ToResponseOrNull(procedure.BodySite),
    };
}
