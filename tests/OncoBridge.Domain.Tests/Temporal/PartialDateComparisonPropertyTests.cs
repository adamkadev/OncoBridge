using CsCheck;
using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Temporal;

/// <summary>
/// Properties that must hold for temporal comparison across arbitrary values.
/// </summary>
/// <remarks>
/// <para>
/// Property-based testing is used here and nowhere else in Phase 1, because this is the one place
/// it genuinely pays: comparison is a partial order over intervals of differing precision, and the
/// interesting failures are combinations of precision, boundary and offset that nobody thinks to
/// write down as examples. Example tests cover the cases we predicted; these cover the ones we did
/// not.
/// </para>
/// <para>
/// Ranges are kept to 2000-2030 and days to 1-28 so that generation never produces an invalid
/// calendar date. Calendar extremes are covered by explicit example tests instead.
/// </para>
/// </remarks>
public sealed class PartialDateComparisonPropertyTests
{
    private static readonly Gen<PartialDate> AnyPartialDate = Gen.OneOf(
        Gen.Int[2000, 2030].Select(PartialDate.FromYear),
        Gen.Select(Gen.Int[2000, 2030], Gen.Int[1, 12], (y, m) => PartialDate.FromYearMonth(y, m)),
        Gen.Select(
            Gen.Int[2000, 2030], Gen.Int[1, 12], Gen.Int[1, 28],
            (y, m, d) => PartialDate.FromDate(y, m, d)),
        Gen.Select(
            Gen.Int[2000, 2030], Gen.Int[1, 12], Gen.Int[1, 28], Gen.Int[0, 23], Gen.Int[-12, 14],
            (y, m, d, h, offsetHours) => PartialDate.FromInstant(
                new DateTimeOffset(y, m, d, h, 0, 0, TimeSpan.FromHours(offsetHours)))));

    /// <summary>Every value denotes the same span of time as itself.</summary>
    [Fact]
    public void Comparison_is_reflexive() =>
        AnyPartialDate.Sample(
            date => Assert.Equal(TemporalComparison.Same, PartialDate.Compare(date, date)),
            iter: 2_000);

    /// <summary>
    /// Reversing the operands must invert the result exactly. An implementation that reported
    /// <c>Before</c> in one direction and <c>Indeterminate</c> in the other would be incoherent.
    /// </summary>
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

    /// <summary>
    /// <c>Before</c> must be transitive. This is the property that makes the relation a usable
    /// partial order rather than an arbitrary predicate.
    /// </summary>
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

    /// <summary>
    /// Values written identically must always compare as the same moment. The converse does not
    /// hold — two instants with different offsets denote one moment but are written differently —
    /// so only this direction is asserted.
    /// </summary>
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

    /// <summary>
    /// A definite ordering between a floating value and an instant must hold for <i>every</i> UTC
    /// offset the floating value could legally have carried.
    /// </summary>
    /// <remarks>
    /// This is the property that verifies the offset-widening arithmetic, and it derives the
    /// floating value's range independently of the implementation rather than restating it. Getting
    /// the sign of the offset backwards is the easy mistake here, and this catches it: a value
    /// whose ordering flips at some legal offset must never have been reported as ordered.
    /// </remarks>
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

                // -12:00 .. +14:00 in whole hours; utc = local - offset.
                for (int offsetHours = -12; offsetHours <= 14; offsetHours++)
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

    /// <summary>
    /// The calendar range a floating value denotes, derived independently of the production code so
    /// the property above is a genuine cross-check rather than a restatement.
    /// </summary>
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
