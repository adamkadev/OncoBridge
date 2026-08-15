using CsCheck;
using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Temporal;

public sealed class PartialDateComparisonPropertyTests
{
    private static readonly Gen<PartialDate> AnyPartialDate = Gen.OneOf(
        Gen.Int[2000, 2030].Select(PartialDate.FromYear),
        Gen.Select(Gen.Int[2000, 2030], Gen.Int[1, 12], (y, m) => PartialDate.FromYearMonth(y, m)),
        Gen.Select(
            Gen.Int[2000, 2030], Gen.Int[1, 12], Gen.Int[1, 28],
            (y, m, d) => PartialDate.FromDate(y, m, d)),
        Gen.Select(
            Gen.Int[2000, 2030], Gen.Int[1, 12], Gen.Int[1, 28], Gen.Int[0, 23], Gen.Int[-14, 14],
            (y, m, d, h, offsetHours) => PartialDate.FromInstant(
                new DateTimeOffset(y, m, d, h, 0, 0, TimeSpan.FromHours(offsetHours)))));

    [Fact]
    public void Comparison_is_reflexive() =>
        AnyPartialDate.Sample(
            date => Assert.Equal(TemporalComparison.Same, PartialDate.Compare(date, date)),
            iter: 2_000);

    [Fact]
    public void Reversing_the_operands_inverts_the_result() =>
        Gen.Select(AnyPartialDate, AnyPartialDate).Sample(
            pair =>
            {
                (PartialDate left, PartialDate right) = pair;

                TemporalComparison forward = PartialDate.Compare(left, right);
                TemporalComparison reverse = PartialDate.Compare(right, left);

                TemporalComparison expected = forward switch
                {
                    TemporalComparison.Before => TemporalComparison.After,
                    TemporalComparison.After => TemporalComparison.Before,
                    TemporalComparison.Same => TemporalComparison.Same,
                    _ => TemporalComparison.Indeterminate,
                };

                Assert.Equal(expected, reverse);
            },
            iter: 5_000);

    [Fact]
    public void Before_is_transitive() =>
        Gen.Select(AnyPartialDate, AnyPartialDate, AnyPartialDate).Sample(
            triple =>
            {
                (PartialDate a, PartialDate b, PartialDate c) = triple;

                bool aBeforeB = PartialDate.Compare(a, b) == TemporalComparison.Before;
                bool bBeforeC = PartialDate.Compare(b, c) == TemporalComparison.Before;

                if (aBeforeB && bBeforeC)
                {
                    Assert.Equal(TemporalComparison.Before, PartialDate.Compare(a, c));
                }
            },
            iter: 10_000);

    [Fact]
    public void Representational_equality_implies_temporal_sameness() =>
        Gen.Select(AnyPartialDate, AnyPartialDate).Sample(
            pair =>
            {
                (PartialDate left, PartialDate right) = pair;

                if (left.Equals(right))
                {
                    Assert.Equal(TemporalComparison.Same, PartialDate.Compare(left, right));
                }
            },
            iter: 5_000);

    [Fact]
    public void An_ordering_against_an_instant_holds_at_every_legal_offset() =>
        Gen.Select(AnyPartialDate, AnyPartialDate).Sample(
            pair =>
            {
                (PartialDate floating, PartialDate instant) = pair;

                if (floating.IsInstant || !instant.IsInstant)
                {
                    return;
                }

                TemporalComparison result = PartialDate.Compare(floating, instant);
                if (result is not (TemporalComparison.Before or TemporalComparison.After))
                {
                    return;
                }

                (DateTime localStart, DateTime localEnd) = LocalRange(floating);
                DateTime instantUtc = instant.Instant!.Value.UtcDateTime;

                for (int offsetHours = -14; offsetHours <= 14; offsetHours++)
                {
                    TimeSpan offset = TimeSpan.FromHours(offsetHours);
                    DateTime utcStart = localStart - offset;
                    DateTime utcEnd = localEnd - offset;

                    if (result == TemporalComparison.Before)
                    {
                        Assert.True(
                            utcEnd < instantUtc,
                            $"'{floating}' was reported Before '{instant}', but at offset "
                                + $"{offsetHours:+00;-00}:00 its range ends at {utcEnd:O}, "
                                + $"which is not before {instantUtc:O}.");
                    }
                    else
                    {
                        Assert.True(
                            utcStart > instantUtc,
                            $"'{floating}' was reported After '{instant}', but at offset "
                                + $"{offsetHours:+00;-00}:00 its range starts at {utcStart:O}, "
                                + $"which is not after {instantUtc:O}.");
                    }
                }
            },
            iter: 10_000);

    private static (DateTime Start, DateTime End) LocalRange(PartialDate date) => date.Precision switch
    {
        DatePrecision.Year => (
            new DateTime(date.Year, 1, 1),
            new DateTime(date.Year, 12, 31, 23, 59, 59).AddTicks(TimeSpan.TicksPerSecond - 1)),

        DatePrecision.Month => (
            new DateTime(date.Year, date.Month!.Value, 1),
            new DateTime(
                date.Year,
                date.Month!.Value,
                DateTime.DaysInMonth(date.Year, date.Month!.Value),
                23, 59, 59).AddTicks(TimeSpan.TicksPerSecond - 1)),

        DatePrecision.Day => (
            new DateTime(date.Year, date.Month!.Value, date.Day!.Value),
            new DateTime(date.Year, date.Month!.Value, date.Day!.Value, 23, 59, 59)
                .AddTicks(TimeSpan.TicksPerSecond - 1)),

        _ => throw new InvalidOperationException("Only floating precisions have a calendar range."),
    };
}
