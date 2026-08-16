using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Normalization;

public sealed record NormalizationResult
{
    public required IReadOnlyList<Patient> Patients { get; init; }

    public required IReadOnlyList<PrimaryCancerDiagnosis> PrimaryCancerDiagnoses { get; init; }

    public required IReadOnlyList<CancerStaging> CancerStagings { get; init; }

    public required IReadOnlyList<CancerSurgicalProcedure> CancerSurgicalProcedures { get; init; }

    public required IReadOnlyList<Lineage> Lineage { get; init; }
}
