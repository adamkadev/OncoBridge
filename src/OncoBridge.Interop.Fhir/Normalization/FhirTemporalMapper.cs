using System.Globalization;
using Hl7.Fhir.Model;
using OncoBridge.Domain.Temporal;

namespace OncoBridge.Interop.Fhir.Normalization;

internal static class FhirTemporalMapper
{
    private const int YearLength = 4;

    private const int YearMonthLength = 7;

    private const int FullDateLength = 10;

    private const string FullDateFormat = "yyyy-MM-dd";

    private const char TimeSeparator = 'T';

    private const char ComponentSeparator = '-';

    private const char UtcDesignator = 'Z';

    private const int MinimumYear = 1;

    private const int MaximumYear = 9999;

    private const int MinimumMonth = 1;

    private const int MaximumMonth = 12;

    private static readonly char[] OffsetSigns = ['+', '-'];

    internal static PartialDate? ToPartialDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Contains(TimeSeparator) ? ToInstant(value) : ToCalendarDate(value);
    }

    internal static PartialDate? ToPartialDate(DataType? value) => value switch
    {
        FhirDateTime dateTime => ToPartialDate(dateTime.Value),
        Instant instant => instant.Value is { } moment ? PartialDate.FromInstant(moment) : null,
        _ => null,
    };

    internal static PartialPeriod? ToPartialPeriod(Period? period)
    {
        if (period is null)
        {
            return null;
        }

        PartialDate? start = ToPartialDate(period.Start);
        PartialDate? end = ToPartialDate(period.End);

        if (start is null && end is null)
        {
            return null;
        }

        if (start is not null && end is not null
            && PartialDate.Compare(end, start) == TemporalComparison.Before)
        {
            return null;
        }

        return PartialPeriod.Create(start, end);
    }

    internal static TemporalOccurrence? ToOccurrence(DataType? onset) => onset switch
    {
        FhirDateTime dateTime =>
            ToPartialDate(dateTime.Value) is { } date ? TemporalOccurrence.FromDate(date) : null,
        Period period =>
            ToPartialPeriod(period) is { } span ? TemporalOccurrence.FromPeriod(span) : null,
        _ => null,
    };

    private static PartialDate? ToCalendarDate(string value)
    {
        if (value.Length == YearLength)
        {
            return TryReadComponent(value, MinimumYear, MaximumYear, out int year)
                ? PartialDate.FromYear(year)
                : null;
        }

        if (value.Length == YearMonthLength && value[YearLength] == ComponentSeparator)
        {
            return TryReadComponent(value.AsSpan(0, YearLength), MinimumYear, MaximumYear, out int year)
                && TryReadComponent(value.AsSpan(YearLength + 1), MinimumMonth, MaximumMonth, out int month)
                    ? PartialDate.FromYearMonth(year, month)
                    : null;
        }

        if (value.Length == FullDateLength)
        {
            return DateOnly.TryParseExact(
                value, FullDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
                    ? PartialDate.FromDate(date.Year, date.Month, date.Day)
                    : null;
        }

        return null;
    }

    private static PartialDate? ToInstant(string value) =>
        HasExplicitOffset(value)
        && DateTimeOffset.TryParse(
            value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset instant)
                ? PartialDate.FromInstant(instant)
                : null;

    private static bool HasExplicitOffset(string value) =>
        value[^1] == UtcDesignator || value.LastIndexOfAny(OffsetSigns) > value.IndexOf(TimeSeparator);

    private static bool TryReadComponent(
        ReadOnlySpan<char> text, int minimum, int maximum, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value)
        && value >= minimum
        && value <= maximum;
}
