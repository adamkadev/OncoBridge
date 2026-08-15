using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Temporal;

/// <summary>
/// Interval semantics: missing boundaries stay missing, precision survives, and only genuine
/// structural contradictions are rejected.
/// </summary>
public sealed class PartialPeriodTests
{
    [Fact]
    public void A_period_keeps_both_boundaries_at_their_own_precision()
    {
        PartialPeriod period = PartialPeriod.Between(
            PartialDate.FromYear(2019),
            PartialDate.FromDate(2019, 3, 14));

        Assert.Equal(DatePrecision.Year, period.Start!.Precision);
        Assert.Equal(DatePrecision.Day, period.End!.Precision);
        Assert.True(period.IsFullyBounded);
    }

    /// <summary>An interval is a different assertion from a point and must not decay into one.</summary>
    [Fact]
    public void A_period_is_not_collapsed_to_its_start()
    {
        PartialDate start = PartialDate.FromDate(2019, 3, 14);
        PartialDate end = PartialDate.FromDate(2019, 6, 30);

        PartialPeriod period = PartialPeriod.Between(start, end);

        Assert.Equal(start, period.Start);
        Assert.Equal(end, period.End);
        Assert.NotEqual(period.Start, period.End);
    }

    [Fact]
    public void An_ongoing_period_keeps_its_end_absent_rather_than_inventing_one()
    {
        PartialPeriod period = PartialPeriod.StartingAt(PartialDate.FromYearMonth(2019, 3));

        Assert.True(period.HasKnownStart);
        Assert.False(period.HasKnownEnd);
        Assert.True(period.IsUnboundedEnd);
        Assert.Null(period.End);
        Assert.False(period.IsFullyBounded);
    }

    [Fact]
    public void A_period_with_an_unstated_start_keeps_it_absent()
    {
        PartialPeriod period = PartialPeriod.EndingAt(PartialDate.FromYear(2020));

        Assert.True(period.IsUnboundedStart);
        Assert.Null(period.Start);
        Assert.True(period.HasKnownEnd);
    }

    [Fact]
    public void A_period_stating_neither_boundary_is_rejected()
    {
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => PartialPeriod.Create(null, null));

        Assert.Contains("at least one boundary", exception.Message, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------
    // The ordering invariant: only a DEFINITE contradiction is rejected.
    // ---------------------------------------------------------------------

    [Fact]
    public void An_end_definitely_before_the_start_is_rejected()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => PartialPeriod.Between(PartialDate.FromYear(2020), PartialDate.FromYear(2019)));

        Assert.Contains("definitely before", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_end_definitely_before_the_start_is_rejected_across_precisions() =>
        Assert.Throws<ArgumentException>(
            () => PartialPeriod.Between(
                PartialDate.FromDate(2019, 6, 1), PartialDate.FromYearMonth(2019, 3)));

    /// <summary>
    /// A zero-length period is a coherent assertion — something that began and ended within the
    /// same stated span — so it is accepted.
    /// </summary>
    [Fact]
    public void A_zero_length_period_is_accepted()
    {
        PartialDate sameDay = PartialDate.FromDate(2019, 3, 14);

        PartialPeriod period = PartialPeriod.Between(sameDay, sameDay);

        Assert.Equal(TemporalComparison.Same, PartialDate.Compare(period.Start!, period.End!));
    }

    /// <summary>
    /// The important half of the invariant. These boundaries overlap, so no ordering can be proven
    /// between them — and an unprovable ordering is not a contradiction. Rejecting it would be the
    /// very fabrication this model exists to prevent.
    /// </summary>
    [Fact]
    public void An_ambiguous_ordering_between_boundaries_is_accepted_not_rejected()
    {
        PartialPeriod period = PartialPeriod.Between(
            PartialDate.FromYear(2019),
            PartialDate.FromYearMonth(2019, 3));

        Assert.Equal(
            TemporalComparison.Indeterminate,
            PartialDate.Compare(period.End!, period.Start!));

        Assert.NotNull(period.Start);
        Assert.NotNull(period.End);
    }

    [Fact]
    public void Null_boundaries_are_rejected_by_the_two_boundary_factories()
    {
        Assert.Throws<ArgumentNullException>(() => PartialPeriod.StartingAt(null!));
        Assert.Throws<ArgumentNullException>(() => PartialPeriod.EndingAt(null!));
        Assert.Throws<ArgumentNullException>(
            () => PartialPeriod.Between(null!, PartialDate.FromYear(2019)));
    }

    // ---------------------------------------------------------------------
    // Equality and rendering.
    // ---------------------------------------------------------------------

    [Fact]
    public void Periods_with_the_same_boundaries_are_equal()
    {
        PartialPeriod first = PartialPeriod.Between(
            PartialDate.FromYear(2019), PartialDate.FromYear(2020));
        PartialPeriod second = PartialPeriod.Between(
            PartialDate.FromYear(2019), PartialDate.FromYear(2020));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first == second);
    }

    [Fact]
    public void A_bounded_period_differs_from_an_open_ended_one_with_the_same_start()
    {
        PartialDate start = PartialDate.FromYear(2019);

        Assert.NotEqual(
            PartialPeriod.Between(start, PartialDate.FromYear(2020)),
            PartialPeriod.StartingAt(start));
    }

    /// <summary>An absent boundary must be visible in the rendering, not silently omitted.</summary>
    [Fact]
    public void Rendering_makes_an_absent_boundary_visible()
    {
        Assert.Equal("2019-03/..", PartialPeriod.StartingAt(PartialDate.FromYearMonth(2019, 3)).ToString());
        Assert.Equal("../2020", PartialPeriod.EndingAt(PartialDate.FromYear(2020)).ToString());
        Assert.Equal(
            "2019/2020",
            PartialPeriod.Between(PartialDate.FromYear(2019), PartialDate.FromYear(2020)).ToString());
    }
}
