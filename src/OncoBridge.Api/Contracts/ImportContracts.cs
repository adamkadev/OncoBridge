using System.Text.Json;

namespace OncoBridge.Api.Contracts;

public sealed record ImportCreatedResponse
{
    public required Guid ImportBatchId { get; init; }
}

public sealed record SourceResourceResponse
{
    public required Guid Id { get; init; }

    public required int EntryIndex { get; init; }

    public string? ResourceType { get; init; }

    public string? SourceLogicalId { get; init; }

    public string? FullUrl { get; init; }

    public string? ContentHash { get; init; }

    public JsonElement? ResourceJson { get; init; }
}

public sealed record ImportResponse
{
    public required Guid ImportBatchId { get; init; }

    public required string SourceSystemLabel { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }

    public string? FileName { get; init; }

    public required string ContentHash { get; init; }

    public string? BundleType { get; init; }

    public required int EntryCount { get; init; }

    public required string Status { get; init; }

    public string? NormalizerVersion { get; init; }

    public DateTimeOffset? NormalizedAt { get; init; }

    public required IReadOnlyList<SourceResourceResponse> SourceResources { get; init; }

    public required IReadOnlyList<Guid> PatientIds { get; init; }
}
