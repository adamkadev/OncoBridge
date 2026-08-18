using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Application.Reading;

public sealed record ImportSummary
{
    public required ImportBatchId Id { get; init; }

    public required string SourceSystemLabel { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }

    public required ContentHash ContentHash { get; init; }

    public string? FileName { get; init; }

    public string? BundleType { get; init; }

    public required int EntryCount { get; init; }

    public required ImportBatchStatus Status { get; init; }

    public string? NormalizerVersion { get; init; }

    public DateTimeOffset? NormalizedAt { get; init; }
}

public sealed record ImportDetails
{
    public required ImportSummary Import { get; init; }

    public required IReadOnlyList<SourceResource> SourceResources { get; init; }

    public required IReadOnlyList<PatientId> PatientIds { get; init; }
}
