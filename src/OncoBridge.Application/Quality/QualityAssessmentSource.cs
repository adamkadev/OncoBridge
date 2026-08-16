using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Quality;

public sealed record QualityAssessmentSource(
    ImportBatchId BatchId,
    IReadOnlyList<SourceResource> SourceResources,
    IReadOnlyList<PrimaryCancerDiagnosis> PrimaryCancerDiagnoses,
    IReadOnlyList<CancerStaging> CancerStagings);
