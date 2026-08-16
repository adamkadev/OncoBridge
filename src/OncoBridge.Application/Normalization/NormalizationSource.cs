using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Normalization;

public sealed record NormalizationSource(
    ImportBatchId BatchId,
    IReadOnlyList<SourceResource> SourceResources);
