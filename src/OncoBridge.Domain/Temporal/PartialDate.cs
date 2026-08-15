namespace OncoBridge.Domain.Temporal;

public sealed class PartialDate : IEquatable<PartialDate>
{
    private static readonly TimeSpan MinUtcOffset = TimeSpan.FromHours(-14);

    private static readonly TimeSpan MaxUtcOffset = TimeSpan.FromHours(14);

    private PartialDate(DatePrecision precision, int year, int? month, int? day, DateTimeOffset? instant)
    {
        Precision = precision;
        Year = year;
        Month = month;
        Day = day;
        Instant = instant;
    }

    public DatePrecision Precision { get; }

    public int Year { get; }

    public int? Month { get; }

    public int? Day { get; }

    public DateTimeOffset? Instant { get; }

    public bool IsInstant => Precision == DatePrecision.Instant;

    public static PartialDate FromYear(int year)
    {
        ValidateYear(year);
        return new PartialDate(DatePrecision.Year, year, month: null, day: null, instant: null);
    }

    public static PartialDate FromYearMonth(int year, int month)
    {
        ValidateYear(year);
        ValidateMonth(month);
        return new PartialDate(DatePrecision.Month, year, month, day: null, instant: null);
    }

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

    public static PartialDate FromInstant(DateTimeOffset instant) =>
        new(DatePrecision.Instant, instant.Year, instant.Month, instant.Day, instant);

    public static TemporalComparison Compare(PartialDate left, PartialDate right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        (DateTime leftStart, DateTime leftEnd) = left.ToRange();
        (DateTime rightStart, DateTime rightEnd) = right.ToRange();

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

    public TemporalComparison CompareWith(PartialDate other) => Compare(this, other);

    private (DateTime Start, DateTime End) ToRange()
    {
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

    private static DateTime EndOfDay(DateTime midnight) =>
        new(midnight.Ticks + TimeSpan.TicksPerDay - 1);

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
            return Instant!.Value.UtcDateTime == other.Instant!.Value.UtcDateTime
                && Instant!.Value.Offset == other.Instant!.Value.Offset;
        }

        return Year == other.Year && Month == other.Month && Day == other.Day;
    }

    public override bool Equals(object? obj) => Equals(obj as PartialDate);

    public override int GetHashCode() =>
        Precision == DatePrecision.Instant
            ? HashCode.Combine(Precision, Instant!.Value.UtcDateTime, Instant!.Value.Offset)
            : HashCode.Combine(Precision, Year, Month, Day);

    public static bool operator ==(PartialDate? left, PartialDate? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(PartialDate? left, PartialDate? right) => !(left == right);

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
