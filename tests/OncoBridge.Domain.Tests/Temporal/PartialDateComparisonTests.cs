using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Temporal;

public sealed class PartialDateComparisonTests
{
    [Fact]
    public void Year_2019_is_before_year_2020() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(PartialDate.FromYear(2019), PartialDate.FromYear(2020)));

    [Fact]
    public void Year_2020_is_after_year_2019() =>
        Assert.Equal(
            TemporalComparison.After,
            PartialDate.Compare(PartialDate.FromYear(2020), PartialDate.FromYear(2019)));

    [Fact]
    public void Year_2019_is_the_same_as_year_2019() =>
        Assert.Equal(
            TemporalComparison.Same,
            PartialDate.Compare(PartialDate.FromYear(2019), PartialDate.FromYear(2019)));

    [Fact]
    public void Year_2019_against_month_2019_03_is_indeterminate() =>
        Assert.Equal(
            TemporalComparison.Indeterminate,
            PartialDate.Compare(PartialDate.FromYear(2019), PartialDate.FromYearMonth(2019, 3)));

    [Fact]
    public void Month_2019_03_is_before_month_2019_04() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(PartialDate.FromYearMonth(2019, 3), PartialDate.FromYearMonth(2019, 4)));

    [Fact]
    public void Month_2019_03_against_day_2019_03_14_is_indeterminate() =>
        Assert.Equal(
            TemporalComparison.Indeterminate,
            PartialDate.Compare(PartialDate.FromYearMonth(2019, 3), PartialDate.FromDate(2019, 3, 14)));

    [Fact]
    public void The_last_day_of_a_year_is_before_the_first_day_of_the_next() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(PartialDate.FromDate(2019, 12, 31), PartialDate.FromDate(2020, 1, 1)));

    [Fact]
    public void Year_2019_is_before_the_first_day_of_2020() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(PartialDate.FromYear(2019), PartialDate.FromDate(2020, 1, 1)));

    [Fact]
    public void Year_2019_against_its_own_last_day_is_indeterminate() =>
        Assert.Equal(
            TemporalComparison.Indeterminate,
            PartialDate.Compare(PartialDate.FromYear(2019), PartialDate.FromDate(2019, 12, 31)));

    [Fact]
    public void December_is_before_the_following_January() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(PartialDate.FromYearMonth(2019, 12), PartialDate.FromYearMonth(2020, 1)));

    [Fact]
    public void A_leap_day_is_before_the_first_of_March_in_the_same_year() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(PartialDate.FromDate(2020, 2, 29), PartialDate.FromDate(2020, 3, 1)));

    [Fact]
    public void Instants_denoting_the_same_moment_through_different_offsets_compare_as_same()
    {
        PartialDate plusTwo = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 10, 0, 0, TimeSpan.FromHours(2)));
        PartialDate utc = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 8, 0, 0, TimeSpan.Zero));

        Assert.Equal(TemporalComparison.Same, PartialDate.Compare(plusTwo, utc));
    }

    [Fact]
    public void Offsets_are_honoured_rather_than_wall_clock_values()
    {
        PartialDate earlierWallClock = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 9, 0, 0, TimeSpan.FromHours(-5)));
        PartialDate laterWallClock = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 12, 0, 0, TimeSpan.FromHours(2)));

        Assert.Equal(14, earlierWallClock.Instant!.Value.UtcDateTime.Hour);
        Assert.Equal(10, laterWallClock.Instant!.Value.UtcDateTime.Hour);
        Assert.Equal(TemporalComparison.After, PartialDate.Compare(earlierWallClock, laterWallClock));
    }

    [Fact]
    public void An_earlier_instant_is_before_a_later_one() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 14, 8, 0, 0, TimeSpan.Zero)),
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 14, 9, 0, 0, TimeSpan.Zero))));

    [Fact]
    public void A_floating_year_well_before_an_instant_still_compares_as_before() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(
                PartialDate.FromYear(2019),
                PartialDate.FromInstant(new DateTimeOffset(2021, 6, 1, 10, 0, 0, TimeSpan.Zero))));

    [Fact]
    public void A_floating_year_containing_an_instant_is_indeterminate() =>
        Assert.Equal(
            TemporalComparison.Indeterminate,
            PartialDate.Compare(
                PartialDate.FromYear(2019),
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 14, 10, 0, 0, TimeSpan.Zero))));

    [Fact]
    public void A_floating_day_against_an_instant_inside_the_offset_window_is_indeterminate() =>
        Assert.Equal(
            TemporalComparison.Indeterminate,
            PartialDate.Compare(
                PartialDate.FromDate(2019, 3, 14),
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 15, 6, 0, 0, TimeSpan.Zero))));

    [Fact]
    public void A_floating_value_is_never_the_same_as_an_instant()
    {
        TemporalComparison result = PartialDate.Compare(
            PartialDate.FromDate(2019, 3, 14),
            PartialDate.FromInstant(new DateTimeOffset(2019, 3, 14, 0, 0, 0, TimeSpan.Zero)));

        Assert.NotEqual(TemporalComparison.Same, result);
        Assert.Equal(TemporalComparison.Indeterminate, result);
    }

    [Fact]
    public void An_instant_at_the_minimum_legal_offset_retains_it()
    {
        PartialDate date = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 10, 0, 0, TimeSpan.FromHours(-14)));

        Assert.Equal(TimeSpan.FromHours(-14), date.Instant!.Value.Offset);
        Assert.Equal("2019-03-14T10:00:00-14:00", date.ToString());
        Assert.Equal(new DateTime(2019, 3, 15, 0, 0, 0), date.Instant!.Value.UtcDateTime);
    }

    [Fact]
    public void An_instant_at_the_maximum_legal_offset_retains_it()
    {
        PartialDate date = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 10, 0, 0, TimeSpan.FromHours(14)));

        Assert.Equal(TimeSpan.FromHours(14), date.Instant!.Value.Offset);
        Assert.Equal("2019-03-14T10:00:00+14:00", date.ToString());
        Assert.Equal(new DateTime(2019, 3, 13, 20, 0, 0), date.Instant!.Value.UtcDateTime);
    }

    [Fact]
    public void Instants_twenty_eight_hours_apart_in_offset_may_denote_the_same_moment() =>
        Assert.Equal(
            TemporalComparison.Same,
            PartialDate.Compare(
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 14, 0, 0, 0, TimeSpan.FromHours(-14))),
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 15, 4, 0, 0, TimeSpan.FromHours(14)))));

    [Fact]
    public void A_floating_day_is_indeterminate_against_an_instant_reachable_only_at_minus_fourteen()
    {
        TemporalComparison result = PartialDate.Compare(
            PartialDate.FromDate(2019, 3, 14),
            PartialDate.FromInstant(new DateTimeOffset(2019, 3, 15, 13, 0, 0, TimeSpan.Zero)));

        Assert.Equal(TemporalComparison.Indeterminate, result);
    }

    [Fact]
    public void A_floating_day_is_indeterminate_against_an_instant_reachable_only_at_plus_fourteen()
    {
        TemporalComparison result = PartialDate.Compare(
            PartialDate.FromDate(2019, 3, 14),
            PartialDate.FromInstant(new DateTimeOffset(2019, 3, 13, 11, 0, 0, TimeSpan.Zero)));

        Assert.Equal(TemporalComparison.Indeterminate, result);
    }

    [Fact]
    public void A_floating_day_is_still_ordered_beyond_the_widest_offset_window()
    {
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(
                PartialDate.FromDate(2019, 3, 14),
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 15, 14, 0, 0, TimeSpan.Zero))));

        Assert.Equal(
            TemporalComparison.After,
            PartialDate.Compare(
                PartialDate.FromDate(2019, 3, 14),
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 13, 9, 0, 0, TimeSpan.Zero))));
    }

    [Fact]
    public void Comparing_at_the_start_of_the_calendar_does_not_overflow()
    {
        TemporalComparison result = PartialDate.Compare(
            PartialDate.FromYear(1),
            PartialDate.FromInstant(new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        Assert.Equal(TemporalComparison.Indeterminate, result);
    }

    [Fact]
    public void Comparing_at_the_end_of_the_calendar_does_not_overflow()
    {
        TemporalComparison result = PartialDate.Compare(
            PartialDate.FromYear(9999),
            PartialDate.FromInstant(new DateTimeOffset(9999, 12, 31, 23, 0, 0, TimeSpan.Zero)));

        Assert.Equal(TemporalComparison.Indeterminate, result);
    }

    [Fact]
    public void Comparing_against_null_is_rejected()
    {
        PartialDate date = PartialDate.FromYear(2019);

        Assert.Throws<ArgumentNullException>(() => PartialDate.Compare(date, null!));
        Assert.Throws<ArgumentNullException>(() => PartialDate.Compare(null!, date));
    }

    [Fact]
    public void CompareWith_matches_the_static_comparison()
    {
        PartialDate left = PartialDate.FromYear(2019);
        PartialDate right = PartialDate.FromYear(2020);

        Assert.Equal(PartialDate.Compare(left, right), left.CompareWith(right));
    }
}
