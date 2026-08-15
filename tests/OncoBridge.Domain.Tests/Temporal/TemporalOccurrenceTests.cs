using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Temporal;

public sealed class TemporalOccurrenceTests
{
    [Fact]
    public void A_point_occurrence_holds_a_date_and_no_period()
    {
        TemporalOccurrence occurrence =
            TemporalOccurrence.FromDate(PartialDate.FromYearMonth(2019, 3));

        Assert.Equal(TemporalOccurrenceKind.Date, occurrence.Kind);
        Assert.NotNull(occurrence.Date);
        Assert.Null(occurrence.Period);
    }

    [Fact]
    public void An_interval_occurrence_holds_a_period_and_no_date()
    {
        TemporalOccurrence occurrence = TemporalOccurrence.FromPeriod(
            PartialPeriod.Between(PartialDate.FromYear(2019), PartialDate.FromYear(2020)));

        Assert.Equal(TemporalOccurrenceKind.Period, occurrence.Kind);
        Assert.NotNull(occurrence.Period);
        Assert.Null(occurrence.Date);
    }

    [Fact]
    public void A_point_and_an_interval_are_never_equal()
    {
        PartialDate date = PartialDate.FromDate(2019, 3, 14);

        TemporalOccurrence point = TemporalOccurrence.FromDate(date);
        TemporalOccurrence interval = TemporalOccurrence.FromPeriod(PartialPeriod.StartingAt(date));

        Assert.NotEqual(point, interval);
        Assert.NotEqual(point.Kind, interval.Kind);
    }

    [Fact]
    public void Occurrences_holding_the_same_value_are_equal()
    {
        TemporalOccurrence first = TemporalOccurrence.FromDate(PartialDate.FromYear(2019));
        TemporalOccurrence second = TemporalOccurrence.FromDate(PartialDate.FromYear(2019));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Null_is_rejected_by_both_factories()
    {
        Assert.Throws<ArgumentNullException>(() => TemporalOccurrence.FromDate(null!));
        Assert.Throws<ArgumentNullException>(() => TemporalOccurrence.FromPeriod(null!));
    }

    [Fact]
    public void Rendering_reflects_whichever_form_is_present()
    {
        Assert.Equal("2019-03", TemporalOccurrence.FromDate(PartialDate.FromYearMonth(2019, 3)).ToString());
        Assert.Equal(
            "2019/..",
            TemporalOccurrence.FromPeriod(PartialPeriod.StartingAt(PartialDate.FromYear(2019))).ToString());
    }
}
