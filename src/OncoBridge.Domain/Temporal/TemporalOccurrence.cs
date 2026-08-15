namespace OncoBridge.Domain.Temporal;

public enum TemporalOccurrenceKind
{
    Date,

    Period,
}

public sealed class TemporalOccurrence : IEquatable<TemporalOccurrence>
{
    private TemporalOccurrence(PartialDate? date, PartialPeriod? period)
    {
        Date = date;
        Period = period;
    }

    public PartialDate? Date { get; }

    public PartialPeriod? Period { get; }

    public TemporalOccurrenceKind Kind =>
        Date is not null ? TemporalOccurrenceKind.Date : TemporalOccurrenceKind.Period;

    public static TemporalOccurrence FromDate(PartialDate date)
    {
        ArgumentNullException.ThrowIfNull(date);
        return new TemporalOccurrence(date, period: null);
    }

    public static TemporalOccurrence FromPeriod(PartialPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);
        return new TemporalOccurrence(date: null, period);
    }

    public bool Equals(TemporalOccurrence? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || (Date == other.Date && Period == other.Period);
    }

    public override bool Equals(object? obj) => Equals(obj as TemporalOccurrence);

    public override int GetHashCode() => HashCode.Combine(Date, Period);

    public override string ToString() => Date?.ToString() ?? Period!.ToString();
}
