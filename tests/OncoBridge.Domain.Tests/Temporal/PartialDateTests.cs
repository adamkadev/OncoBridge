using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Temporal;

/// <summary>
/// Construction, precision preservation and representation of <see cref="PartialDate"/>.
/// </summary>
/// <remarks>
/// The single behaviour under test throughout is that stated precision survives. A value written
/// as <c>2019</c> must never become <c>2019-01-01</c> anywhere in its lifetime, because that
/// fabricates an assertion the source never made.
/// </remarks>
public sealed class PartialDateTests
{
    [Fact]
    public void Year_precision_keeps_year_precision_and_exposes_no_month_or_day()
    {
        PartialDate date = PartialDate.FromYear(2019);

        Assert.Equal(DatePrecision.Year, date.Precision);
        Assert.Equal(2019, date.Year);
        Assert.Null(date.Month);
        Assert.Null(date.Day);
        Assert.Null(date.Instant);
        Assert.Equal("2019", date.ToString());
    }

    [Fact]
    public void Month_precision_keeps_month_precision_and_exposes_no_day()
    {
        PartialDate date = PartialDate.FromYearMonth(2019, 3);

        Assert.Equal(DatePrecision.Month, date.Precision);
        Assert.Equal(3, date.Month);
        Assert.Null(date.Day);
        Assert.Equal("2019-03", date.ToString());
    }

    [Fact]
    public void Day_precision_keeps_day_precision()
    {
        PartialDate date = PartialDate.FromDate(2019, 3, 14);

        Assert.Equal(DatePrecision.Day, date.Precision);
        Assert.Equal(14, date.Day);
        Assert.Null(date.Instant);
        Assert.Equal("2019-03-14", date.ToString());
    }

    [Fact]
    public void Instant_precision_retains_the_stated_offset_rather_than_normalising_to_utc()
    {
        DateTimeOffset value = new(2019, 3, 14, 10, 0, 0, TimeSpan.FromHours(2));
        PartialDate date = PartialDate.FromInstant(value);

        Assert.Equal(DatePrecision.Instant, date.Precision);
        Assert.True(date.IsInstant);
        Assert.Equal(TimeSpan.FromHours(2), date.Instant!.Value.Offset);
        Assert.Equal("2019-03-14T10:00:00+02:00", date.ToString());
    }

    /// <summary>
    /// The central guarantee: nothing in the type produces a padded calendar value from a
    /// less-precise one.
    /// </summary>
    [Fact]
    public void Year_precision_never_renders_as_a_padded_calendar_date()
    {
        PartialDate date = PartialDate.FromYear(2019);

        Assert.Equal("2019", date.ToString());
        Assert.DoesNotContain("01-01", date.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_000)]
    public void Year_outside_the_supported_range_is_rejected(int year) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PartialDate.FromYear(year));

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Month_outside_1_to_12_is_rejected(int month) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PartialDate.FromYearMonth(2019, month));

    [Theory]
    [InlineData(2019, 2, 29)] // 2019 is not a leap year
    [InlineData(2019, 4, 31)]
    [InlineData(2019, 1, 0)]
    [InlineData(2019, 1, 32)]
    public void A_day_that_does_not_exist_in_that_month_is_rejected(int year, int month, int day) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PartialDate.FromDate(year, month, day));

    [Fact]
    public void A_leap_day_is_accepted_in_a_leap_year()
    {
        PartialDate date = PartialDate.FromDate(2020, 2, 29);

        Assert.Equal(29, date.Day);
    }

    [Fact]
    public void Equality_is_representational_so_different_precisions_are_never_equal()
    {
        Assert.NotEqual(PartialDate.FromYear(2019), PartialDate.FromYearMonth(2019, 1));
        Assert.NotEqual(PartialDate.FromYearMonth(2019, 1), PartialDate.FromDate(2019, 1, 1));
    }

    [Fact]
    public void Equal_values_agree_on_hash_code()
    {
        PartialDate first = PartialDate.FromDate(2019, 3, 14);
        PartialDate second = PartialDate.FromDate(2019, 3, 14);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first == second);
        Assert.False(first != second);
    }

    /// <summary>
    /// The distinction between the two notions of sameness, asserted directly: these two values
    /// denote the same moment but were not written the same way.
    /// </summary>
    [Fact]
    public void Instants_with_the_same_moment_but_different_offsets_are_not_representationally_equal()
    {
        PartialDate plusTwo = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 10, 0, 0, TimeSpan.FromHours(2)));
        PartialDate utc = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 8, 0, 0, TimeSpan.Zero));

        Assert.NotEqual(plusTwo, utc);
        Assert.Equal(TemporalComparison.Same, PartialDate.Compare(plusTwo, utc));
    }

    [Fact]
    public void A_date_is_not_equal_to_null()
    {
        PartialDate date = PartialDate.FromYear(2019);

        Assert.False(date.Equals(null));
        Assert.False(date == null);
        Assert.True(date != null);
    }
}
