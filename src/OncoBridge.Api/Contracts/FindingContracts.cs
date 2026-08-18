namespace OncoBridge.Api.Contracts;

public sealed record FindingTargetResponse
{
    public required string Kind { get; init; }

    public required Guid Id { get; init; }

    public string? DomainEntityType { get; init; }
}

public sealed record FindingResponse
{
    public required string CheckId { get; init; }

    public required string Category { get; init; }

    public required string Severity { get; init; }

    public required string Message { get; init; }

    public required FindingTargetResponse Target { get; init; }

    public required string Citation { get; init; }

    public string? Expected { get; init; }

    public string? Actual { get; init; }
}
