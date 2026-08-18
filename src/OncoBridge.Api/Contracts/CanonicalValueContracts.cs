namespace OncoBridge.Api.Contracts;

public sealed record CodedConceptResponse
{
    public required string System { get; init; }

    public required string Code { get; init; }

    public string? Display { get; init; }
}

public sealed record PartialDateResponse
{
    public required string Value { get; init; }

    public required string Precision { get; init; }
}

public sealed record PartialPeriodResponse
{
    public PartialDateResponse? Start { get; init; }

    public PartialDateResponse? End { get; init; }
}

public sealed record TemporalOccurrenceResponse
{
    public required string Kind { get; init; }

    public PartialDateResponse? Date { get; init; }

    public PartialPeriodResponse? Period { get; init; }
}
