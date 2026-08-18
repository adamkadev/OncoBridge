using OncoBridge.Domain.Oncology;

namespace OncoBridge.Application.Reading;

public sealed record PatientRecord
{
    public required Patient Patient { get; init; }

    public required IReadOnlyList<PrimaryCancerDiagnosis> PrimaryCancerDiagnoses { get; init; }

    public required IReadOnlyList<CancerStaging> CancerStagings { get; init; }

    public required IReadOnlyList<CancerSurgicalProcedure> CancerSurgicalProcedures { get; init; }
}
