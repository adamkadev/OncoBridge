namespace OncoBridge.Api.Contracts;

public sealed record PatientResponse
{
    public required Guid Id { get; init; }

    public string? SourceIdentifier { get; init; }

    public PartialDateResponse? BirthDate { get; init; }

    public CodedConceptResponse? SexAtBirthAsRecorded { get; init; }
}

public sealed record PrimaryCancerDiagnosisResponse
{
    public required Guid Id { get; init; }

    public required Guid PatientId { get; init; }

    public required CodedConceptResponse Code { get; init; }

    public TemporalOccurrenceResponse? Onset { get; init; }

    public CodedConceptResponse? BodySite { get; init; }

    public PartialDateResponse? RecordedDate { get; init; }
}

public sealed record StageCategoryResponse
{
    public required string Axis { get; init; }

    public required CodedConceptResponse Code { get; init; }

    public required Guid SourceResourceId { get; init; }
}

public sealed record CancerStagingResponse
{
    public required Guid Id { get; init; }

    public required Guid PatientId { get; init; }

    public required Guid PrimaryCancerDiagnosisId { get; init; }

    public CodedConceptResponse? StageGroup { get; init; }

    public CodedConceptResponse? Method { get; init; }

    public PartialDateResponse? Effective { get; init; }

    public required IReadOnlyList<StageCategoryResponse> Categories { get; init; }
}

public sealed record CancerSurgicalProcedureResponse
{
    public required Guid Id { get; init; }

    public required Guid PatientId { get; init; }

    public required CodedConceptResponse Code { get; init; }

    public TemporalOccurrenceResponse? Performed { get; init; }

    public CodedConceptResponse? BodySite { get; init; }
}

public sealed record PatientRecordResponse
{
    public required PatientResponse Patient { get; init; }

    public required IReadOnlyList<PrimaryCancerDiagnosisResponse> PrimaryCancerDiagnoses { get; init; }

    public required IReadOnlyList<CancerStagingResponse> CancerStagings { get; init; }

    public required IReadOnlyList<CancerSurgicalProcedureResponse> CancerSurgicalProcedures { get; init; }
}
