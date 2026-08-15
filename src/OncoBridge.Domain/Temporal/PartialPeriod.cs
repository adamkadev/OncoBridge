namespace OncoBridge.Domain.Temporal;

public sealed class PartialPeriod : IEquatable<PartialPeriod>
{
    private PartialPeriod(PartialDate? start, PartialDate? end)
    {
        Start = start;
        End = end;
    }

    public PartialDate? Start { get; }

    public PartialDate? End { get; }

    public bool HasKnownStart => Start is not null;

    public bool HasKnownEnd => End is not null;

    public bool IsUnboundedStart => Start is null;

    public bool IsUnboundedEnd => End is null;

    public bool IsFullyBounded => Start is not null && End is not null;

    public static PartialPeriod Create(PartialDate? start, PartialDate? end)
    {
        if (start is null && end is null)
        {
            throw new ArgumentException(
                "A period must state at least one boundary; one with neither asserts nothing.",
                nameof(start));
        }

        if (start is not null && end is not null)
        {
            TemporalComparison comparison = PartialDate.Compare(end, start);
            if (comparison == TemporalComparison.Before)
            {
                throw new ArgumentException(
                    $"Period end '{end}' is definitely before start '{start}'.", nameof(end));
            }
        }

        return new PartialPeriod(start, end);
    }

    public static PartialPeriod StartingAt(PartialDate start)
    {
        ArgumentNullException.ThrowIfNull(start);
        return new PartialPeriod(start, end: null);
    }

    public static PartialPeriod EndingAt(PartialDate end)
    {
        ArgumentNullException.ThrowIfNull(end);
        return new PartialPeriod(start: null, end);
    }

    public static PartialPeriod Between(PartialDate start, PartialDate end)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        return Create(start, end);
    }

    public bool Equals(PartialPeriod? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || (Start == other.Start && End == other.End);
    }

    public override bool Equals(object? obj) => Equals(obj as PartialPeriod);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(PartialPeriod? left, PartialPeriod? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PartialPeriod? left, PartialPeriod? right) => !(left == right);

    public override string ToString() => $"{Start?.ToString() ?? ".."}/{End?.ToString() ?? ".."}";
}
