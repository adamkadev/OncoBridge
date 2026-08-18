namespace OncoBridge.Api.Contracts;

public sealed record LineageResponse
{
    public required string DomainEntityType { get; init; }

    public required Guid DomainEntityId { get; init; }

    public string? FieldPath { get; init; }

    public required Guid SourceResourceId { get; init; }

    public required string TransformationName { get; init; }

    public required string TransformationVersion { get; init; }
}

public sealed record ProvenanceResponse
{
    public required Guid DomainEntityId { get; init; }

    public required IReadOnlyList<LineageResponse> Records { get; init; }
}
