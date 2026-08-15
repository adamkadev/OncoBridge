namespace OncoBridge.Domain.Temporal;

/// <summary>
/// A date or time stated at whatever precision the source actually supplied,
/// with that precision preserved.
/// </summary>
/// <remarks>
/// <para>
/// Clinical source data routinely states dates at mixed precision. Mapping such a value onto
/// <see cref="DateTime"/> fabricates precision that was never asserted — <c>2019</c> becomes
/// <c>2019-01-01T00:00:00</c> — and every downstream comparison is then confidently wrong.
/// <see cref="PartialDate"/> exists to make that mistake unrepresentable.
/// </para>
/// <para><b>Two notions of sameness, deliberately kept apart:</b></para>
/// <list type="bullet">
///   <item>
///     <description>
///     <see cref="Equals(PartialDate?)"/> is <i>representational</i>: it asks whether two values
///     were written identically. <c>2019-03-14T10:00:00+02:00</c> and <c>2019-03-14T08:00:00+00:00</c>
///     are NOT equal, because their stated offsets differ.
///     </description>
///   </item>
///   <item>
///     <description>
///     <see cref="Compare"/> is <i>temporal</i>: it asks how the represented spans of time relate.
///     Those same two values compare as <see cref="TemporalComparison.Same"/>, because they denote
///     the same instant.
///     </description>
///   </item>
/// </list>
/// <para>
/// Both are correct answers to different questions, and conflating them is exactly the class of
/// bug this type prevents.
/// </para>
/// </remarks>
public sealed class PartialDate : IEquatable<PartialDate>
{
    /// <summary>Smallest UTC offset permitted by ISO 8601 / FHIR.</summary>
    private static readonly TimeSpan MinUtcOffset = TimeSpan.FromHours(-12);

    /// <summary>Largest UTC offset permitted by ISO 8601 / FHIR.</summary>
    private static readonly TimeSpan MaxUtcOffset = TimeSpan.FromHours(14);

    private PartialDate(DatePrecision precision, int year, int? month, int? day, DateTimeOffset? instant)
    {
        Precision = precision;
        Year = year;
        Month = month;
        Day = day;
        Instant = instant;
    }

    /// <summary>The precision at which this value was stated. Never inferred, never widened.</summary>
    public DatePrecision Precision { get; }

    /// <summary>The year. Always present at every precision.</summary>
    public int Year { get; }

    /// <summary>The month, or <see langword="null"/> when <see cref="Precision"/> is <see cref="DatePrecision.Year"/>.</summary>
    public int? Month { get; }

    /// <summary>The day, or <see langword="null"/> at year or month precision.</summary>
    public int? Day { get; }

    /// <summary>
    /// The full instant including its stated UTC offset, or <see langword="null"/> unless
    /// <see cref="Precision"/> is <see cref="DatePrecision.Instant"/>.
    /// </summary>
    /// <remarks>
    /// Year, month and day precisions carry no timezone at all — that is a property of the source
    /// formats, not an omission here. Such values float against the local calendar.
    /// </remarks>
    public DateTimeOffset? Instant { get; }

    /// <summary>Whether this value is anchored to a known UTC offset.</summary>
    public bool IsInstant => Precision == DatePrecision.Instant;

    /// <summary>A value stated as a year only, e.g. <c>2019</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The year is outside 1..9999.</exception>
    public static PartialDate FromYear(int year)
    {
        ValidateYear(year);
        return new PartialDate(DatePrecision.Year, year, month: null, day: null, instant: null);
    }

    /// <summary>A value stated as a year and month, e.g. <c>2019-03</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The year or month is out of range.</exception>
    public static PartialDate FromYearMonth(int year, int month)
    {
        ValidateYear(year);
        ValidateMonth(month);
        return new PartialDate(DatePrecision.Month, year, month, day: null, instant: null);
    }

    /// <summary>A value stated as a calendar day, e.g. <c>2019-03-14</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The year or month is out of range, or the day does not exist in that month.
    /// </exception>
    public static PartialDate FromDate(int year, int month, int day)
    {
        ValidateYear(year);
        ValidateMonth(month);

        int daysInMonth = DateTime.DaysInMonth(year, month);
        if (day < 1 || day > daysInMonth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(day), day, $"Day must be between 1 and {daysInMonth} for {year}-{month:D2}.");
        }

        return new PartialDate(DatePrecision.Day, year, month, day, instant: null);
    }

    /// <summary>A value stated as a full instant with a known UTC offset.</summary>
    /// <remarks>
    /// <paramref name="instant"/> is stored exactly as supplied. The offset is preserved rather
    /// than normalised to UTC, because the stated offset is part of what the source asserted.
    /// </remarks>
    public static PartialDate FromInstant(DateTimeOffset instant) =>
        new(DatePrecision.Instant, instant.Year, instant.Month, instant.Day, instant);

    /// <summary>
    /// Compares two values by the spans of time they represent, returning
    /// <see cref="TemporalComparison.Indeterminate"/> whenever no ordering can be proven.
    /// </summary>
    /// <remarks>
    /// <para><b>How this works.</b> Every value denotes a closed interval:</para>
    /// <list type="bullet">
    ///   <item><description><c>2019</c> denotes <c>[2019-01-01 00:00:00.0000000, 2019-12-31 23:59:59.9999999]</c></description></item>
    ///   <item><description><c>2019-03</c> denotes the whole of March 2019</description></item>
    ///   <item><description><c>2019-03-14</c> denotes the whole of that day</description></item>
    ///   <item><description>An instant denotes a single point</description></item>
    /// </list>
    /// <para>Given intervals <c>a</c> and <c>b</c>:</para>
    /// <list type="bullet">
    ///   <item><description><c>a.End &lt; b.Start</c> → <see cref="TemporalComparison.Before"/></description></item>
    ///   <item><description><c>b.End &lt; a.Start</c> → <see cref="TemporalComparison.After"/></description></item>
    ///   <item><description>identical intervals → <see cref="TemporalComparison.Same"/></description></item>
    ///   <item><description>overlapping but not identical → <see cref="TemporalComparison.Indeterminate"/></description></item>
    /// </list>
    /// <para><b>Mixing floating values with instants.</b> Year, month and day values carry no
    /// timezone, so they cannot be placed on the UTC timeline exactly. When one side is an instant
    /// and the other is not, the floating side is widened by the full range of legal UTC offsets
    /// (-12:00 to +14:00) before comparing. A definite result is therefore returned only when it
    /// holds for <i>every</i> offset the floating value could have had. Comparing two floating
    /// values needs no widening: both sit on the same calendar timeline, so the unknown offset
    /// cancels out.</para>
    /// <para>A consequence worth stating: a floating value and an instant can never compare as
    /// <see cref="TemporalComparison.Same"/>, because a widened interval is never a single point.
    /// That is correct — <c>2019</c> is not provably the same moment as any instant.</para>
    /// </remarks>
    public static TemporalComparison Compare(PartialDate left, PartialDate right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        (DateTime leftStart, DateTime leftEnd) = left.ToRange();
        (DateTime rightStart, DateTime rightEnd) = right.ToRange();

        // Only widen when the two values sit on different timelines.
        if (left.IsInstant != right.IsInstant)
        {
            if (!left.IsInstant)
            {
                (leftStart, leftEnd) = Widen(leftStart, leftEnd);
            }
            else
            {
                (rightStart, rightEnd) = Widen(rightStart, rightEnd);
            }
        }

        if (leftEnd < rightStart)
        {
            return TemporalComparison.Before;
        }

        if (rightEnd < leftStart)
        {
            return TemporalComparison.After;
        }

        if (leftStart == rightStart && leftEnd == rightEnd)
        {
            return TemporalComparison.Same;
        }

        return TemporalComparison.Indeterminate;
    }

    /// <summary>Compares this value with <paramref name="other"/>. See <see cref="Compare"/>.</summary>
    public TemporalComparison CompareWith(PartialDate other) => Compare(this, other);

    /// <summary>
    /// The closed interval this value denotes: for floating precisions on the local calendar,
    /// for instants on the UTC timeline.
    /// </summary>
    private (DateTime Start, DateTime End) ToRange()
    {
        // Ends are derived from the first tick of the last day rather than by adding a unit and
        // stepping back, because adding a year to 9999 overflows DateTime. Computed this way the
        // final instant of year 9999 lands exactly on DateTime.MaxValue.
        switch (Precision)
        {
            case DatePrecision.Year:
                return (new DateTime(Year, 1, 1), EndOfDay(new DateTime(Year, 12, 31)));

            case DatePrecision.Month:
            {
                int lastDay = DateTime.DaysInMonth(Year, Month!.Value);
                return (
                    new DateTime(Year, Month!.Value, 1),
                    EndOfDay(new DateTime(Year, Month!.Value, lastDay)));
            }

            case DatePrecision.Day:
            {
                DateTime start = new(Year, Month!.Value, Day!.Value);
                return (start, EndOfDay(start));
            }

            case DatePrecision.Instant:
            {
                DateTime utc = Instant!.Value.UtcDateTime;
                return (utc, utc);
            }

            default:
                throw new InvalidOperationException($"Unhandled precision '{Precision}'.");
        }
    }

    /// <summary>The last representable tick of the day that <paramref name="midnight"/> begins.</summary>
    private static DateTime EndOfDay(DateTime midnight) =>
        new(midnight.Ticks + TimeSpan.TicksPerDay - 1);

    /// <summary>
    /// Expands a floating calendar interval to every UTC instant it could correspond to,
    /// given that its offset is unknown but must lie within -12:00..+14:00.
    /// </summary>
    /// <remarks>
    /// Because <c>utc = local - offset</c>, the largest positive offset yields the earliest
    /// possible UTC start and the smallest yields the latest possible UTC end.
    /// Saturating arithmetic keeps values at the extremes of the calendar in range.
    /// </remarks>
    private static (DateTime Start, DateTime End) Widen(DateTime start, DateTime end) =>
        (SaturatingAdd(start, -MaxUtcOffset), SaturatingAdd(end, -MinUtcOffset));

    private static DateTime SaturatingAdd(DateTime value, TimeSpan delta)
    {
        if (delta > TimeSpan.Zero && value > DateTime.MaxValue - delta)
        {
            return DateTime.MaxValue;
        }

        if (delta < TimeSpan.Zero && value < DateTime.MinValue - delta)
        {
            return DateTime.MinValue;
        }

        return value + delta;
    }

    private static void ValidateYear(int year)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be between 1 and 9999.");
        }
    }

    private static void ValidateMonth(int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }
    }

    /// <summary>
    /// Representational equality: whether both values were written the same way, including the
    /// stated UTC offset. For temporal equivalence use <see cref="Compare"/> instead.
    /// </summary>
    public bool Equals(PartialDate? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Precision != other.Precision)
        {
            return false;
        }

        if (Precision == DatePrecision.Instant)
        {
            // DateTimeOffset.Equals compares only the instant, so the offset is checked separately
            // to keep this representational.
            return Instant!.Value.UtcDateTime == other.Instant!.Value.UtcDateTime
                && Instant!.Value.Offset == other.Instant!.Value.Offset;
        }

        return Year == other.Year && Month == other.Month && Day == other.Day;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as PartialDate);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        Precision == DatePrecision.Instant
            ? HashCode.Combine(Precision, Instant!.Value.UtcDateTime, Instant!.Value.Offset)
            : HashCode.Combine(Precision, Year, Month, Day);

    /// <summary>Representational equality. See <see cref="Equals(PartialDate?)"/>.</summary>
    public static bool operator ==(PartialDate? left, PartialDate? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Representational inequality. See <see cref="Equals(PartialDate?)"/>.</summary>
    public static bool operator !=(PartialDate? left, PartialDate? right) => !(left == right);

    /// <summary>Renders the value at exactly the precision it was stated, never padded.</summary>
    public override string ToString() => Precision switch
    {
        DatePrecision.Year => Year.ToString("D4", System.Globalization.CultureInfo.InvariantCulture),
        DatePrecision.Month => $"{Year:D4}-{Month!.Value:D2}",
        DatePrecision.Day => $"{Year:D4}-{Month!.Value:D2}-{Day!.Value:D2}",
        DatePrecision.Instant => Instant!.Value.ToString(
            "yyyy-MM-dd'T'HH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException($"Unhandled precision '{Precision}'."),
    };
}
