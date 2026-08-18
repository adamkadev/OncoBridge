using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Imports;

public sealed record IngestedPayload(
    ImportBatch Batch,
    IReadOnlyList<SourceResource> SourceResources);
