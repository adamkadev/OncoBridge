using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Temporal;

/// <summary>
/// Temporal comparison of variable-precision values.
/// </summary>
/// <remarks>
/// This is the most important behaviour in the Phase 1 domain. The rule under test is that a
/// definite ordering is returned only when it can be proven from the ranges the two values denote;
/// everything else is <see cref="TemporalComparison.Indeterminate"/>, and no result ever fabricates
/// an ordering that the stated precisions do not support.
/// </remarks>
public sealed class PartialDateComparisonTests
{
    // ---------------------------------------------------------------------
    // The worked examples specified for Phase 1.
    // ---------------------------------------------------------------------

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

    /// <summary>
    /// The case that motivates the whole design: March 2019 lies inside 2019, so neither precedes
    /// the other and they are not equivalent. Any answer other than indeterminate would be invented.
    /// </summary>
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

    // ---------------------------------------------------------------------
    // Boundaries — where an off-by-one would produce a wrong ordering.
    // ---------------------------------------------------------------------

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

    // ---------------------------------------------------------------------
    // Instants and offsets.
    // ---------------------------------------------------------------------

    [Fact]
    public void Instants_denoting_the_same_moment_through_different_offsets_compare_as_same()
    {
        PartialDate plusTwo = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 10, 0, 0, TimeSpan.FromHours(2)));
        PartialDate utc = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 8, 0, 0, TimeSpan.Zero));

        Assert.Equal(TemporalComparison.Same, PartialDate.Compare(plusTwo, utc));
    }

    /// <summary>
    /// Two wall-clock readings that look ordered but are not, once their offsets are honoured.
    /// Comparing the printed strings would give the wrong answer here.
    /// </summary>
    [Fact]
    public void Offsets_are_honoured_rather_than_wall_clock_values()
    {
        PartialDate earlierWallClock = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 9, 0, 0, TimeSpan.FromHours(-5)));  // 14:00Z
        PartialDate laterWallClock = PartialDate.FromInstant(
            new DateTimeOffset(2019, 3, 14, 12, 0, 0, TimeSpan.FromHours(2)));  // 10:00Z

        Assert.Equal(TemporalComparison.After, PartialDate.Compare(earlierWallClock, laterWallClock));
    }

    [Fact]
    public void An_earlier_instant_is_before_a_later_one() =>
        Assert.Equal(
            TemporalComparison.Before,
            PartialDate.Compare(
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 14, 8, 0, 0, TimeSpan.Zero)),
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 14, 9, 0, 0, TimeSpan.Zero))));

    // ---------------------------------------------------------------------
    // Mixing a floating value with an instant.
    // ---------------------------------------------------------------------

    /// <summary>
    /// A floating value is widened by the full range of legal UTC offsets before being compared
    /// with an instant, so a definite answer is given only when it holds for every offset the
    /// floating value could have had.
    /// </summary>
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

    /// <summary>
    /// Within the offset-uncertainty window the answer must stay indeterminate. A floating day
    /// compared against an instant a few hours after that day's local end could still precede it
    /// or not, depending on the offset the source omitted — so no ordering may be claimed.
    /// </summary>
    [Fact]
    public void A_floating_day_against_an_instant_inside_the_offset_window_is_indeterminate() =>
        Assert.Equal(
            TemporalComparison.Indeterminate,
            PartialDate.Compare(
                PartialDate.FromDate(2019, 3, 14),
                PartialDate.FromInstant(new DateTimeOffset(2019, 3, 15, 6, 0, 0, TimeSpan.Zero))));

    /// <summary>
    /// A floating value can never be provably the same moment as an instant, because a widened
    /// interval is never a single point.
    /// </summary>
    [Fact]
    public void A_floating_value_is_never_the_same_as_an_instant()
    {
        TemporalComparison result = PartialDate.Compare(
            PartialDate.FromDate(2019, 3, 14),
            PartialDate.FromInstant(new DateTimeOffset(2019, 3, 14, 0, 0, 0, TimeSpan.Zero)));

        Assert.NotEqual(TemporalComparison.Same, result);
        Assert.Equal(TemporalComparison.Indeterminate, result);
    }

    // ---------------------------------------------------------------------
    // Extremes of the calendar, where widening could overflow.
    // ---------------------------------------------------------------------

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

    // ---------------------------------------------------------------------
    // Argument handling.
    // ---------------------------------------------------------------------

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
