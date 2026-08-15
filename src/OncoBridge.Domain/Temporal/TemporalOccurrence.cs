namespace OncoBridge.Domain.Temporal;

/// <summary>Which kind of temporal value a <see cref="TemporalOccurrence"/> holds.</summary>
public enum TemporalOccurrenceKind
{
    /// <summary>A single point or partial date.</summary>
    Date,

    /// <summary>An interval.</summary>
    Period,
}

/// <summary>
/// When something happened, stated either as a <see cref="PartialDate"/> or as a
/// <see cref="PartialPeriod"/> — exactly one of the two, never both and never neither.
/// </summary>
/// <remarks>
/// <para>
/// Several concepts record occurrence as either a point or an interval, and the two carry
/// different assertions: "onset was in March 2019" is not the same claim as "onset occurred
/// somewhere between 2019 and 2020". Collapsing an interval to its start would discard that
/// distinction, so this type keeps them apart.
/// </para>
/// <para>
/// It exists only to hold the exactly-one-of invariant in a single place rather than repeating
/// it on every entity that records an occurrence.
/// </para>
/// </remarks>
public sealed class TemporalOccurrence : IEquatable<TemporalOccurrence>
{
    private TemporalOccurrence(PartialDate? date, PartialPeriod? period)
    {
        Date = date;
        Period = period;
    }

    /// <summary>The point value, or <see langword="null"/> when this occurrence is an interval.</summary>
    public PartialDate? Date { get; }

    /// <summary>The interval value, or <see langword="null"/> when this occurrence is a point.</summary>
    public PartialPeriod? Period { get; }

    /// <summary>Which of the two forms is present.</summary>
    public TemporalOccurrenceKind Kind =>
        Date is not null ? TemporalOccurrenceKind.Date : TemporalOccurrenceKind.Period;

    /// <summary>An occurrence stated as a single (possibly imprecise) point in time.</summary>
    public static TemporalOccurrence FromDate(PartialDate date)
    {
        ArgumentNullException.ThrowIfNull(date);
        return new TemporalOccurrence(date, period: null);
    }

    /// <summary>An occurrence stated as an interval.</summary>
    public static TemporalOccurrence FromPeriod(PartialPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);
        return new TemporalOccurrence(date: null, period);
    }

    /// <summary>Representational equality on whichever form is present.</summary>
    public bool Equals(TemporalOccurrence? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || (Date == other.Date && Period == other.Period);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as TemporalOccurrence);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Date, Period);

    /// <inheritdoc/>
    public override string ToString() => Date?.ToString() ?? Period!.ToString();
}
