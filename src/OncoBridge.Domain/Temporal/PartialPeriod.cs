namespace OncoBridge.Domain.Temporal;

/// <summary>
/// A temporal interval whose boundaries may be unknown and may be stated at different precisions.
/// </summary>
/// <remarks>
/// <para>
/// Source data expresses intervals with either boundary absent, and each boundary at its own
/// precision — a period may run from <c>2019</c> to <c>2019-03-14</c>. Both facts are preserved
/// here rather than smoothed away.
/// </para>
/// <para><b>Rules this type exists to enforce (ADR-0005):</b></para>
/// <list type="bullet">
///   <item><description>A period is never collapsed to its start. An interval and a point are different assertions.</description></item>
///   <item><description>A missing boundary stays missing. Nothing is fabricated to make the type total.</description></item>
///   <item><description>Each boundary keeps its own <see cref="PartialDate.Precision"/>.</description></item>
/// </list>
/// <para><b>Invariants — only genuine structural ones are checked:</b></para>
/// <list type="number">
///   <item><description>At least one boundary must be known. A period with neither asserts nothing at all.</description></item>
///   <item><description>
///     When both boundaries are known, the end must not be <i>definitely</i> before the start —
///     that is, only <see cref="TemporalComparison.Before"/> is rejected.
///     <see cref="TemporalComparison.Same"/> is allowed (a zero-length period is meaningful), and
///     <see cref="TemporalComparison.Indeterminate"/> is allowed because an unprovable ordering is
///     not a contradiction. Rejecting ambiguity here would be exactly the fabrication this model
///     is designed to avoid.
///   </description></item>
/// </list>
/// <para><b>Deliberately not implemented in Phase 1:</b> comparison of a period against another
/// period or against a <see cref="PartialDate"/>. No Phase 1 concept needs it, and the semantics
/// (containment vs. overlap vs. ordering) should be settled by the rule that first requires them
/// rather than guessed at now.</para>
/// </remarks>
public sealed class PartialPeriod : IEquatable<PartialPeriod>
{
    private PartialPeriod(PartialDate? start, PartialDate? end)
    {
        Start = start;
        End = end;
    }

    /// <summary>The start boundary, or <see langword="null"/> if the source did not state one.</summary>
    public PartialDate? Start { get; }

    /// <summary>The end boundary, or <see langword="null"/> if the source did not state one.</summary>
    public PartialDate? End { get; }

    /// <summary>Whether a start boundary was stated.</summary>
    public bool HasKnownStart => Start is not null;

    /// <summary>Whether an end boundary was stated.</summary>
    public bool HasKnownEnd => End is not null;

    /// <summary>Whether the interval is open at its start because no start was stated.</summary>
    public bool IsUnboundedStart => Start is null;

    /// <summary>Whether the interval is open at its end because no end was stated — an ongoing period.</summary>
    public bool IsUnboundedEnd => End is null;

    /// <summary>Whether both boundaries were stated.</summary>
    public bool IsFullyBounded => Start is not null && End is not null;

    /// <summary>
    /// Creates a period from the boundaries actually supplied. Either may be
    /// <see langword="null"/>, but not both.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Both boundaries are <see langword="null"/>, or the end is definitely before the start.
    /// </exception>
    public static PartialPeriod Create(PartialDate? start, PartialDate? end)
    {
        if (start is null && end is null)
        {
            throw new ArgumentException(
                "A period must state at least one boundary; one with neither asserts nothing.",
                nameof(start));
        }

        if (start is not null && end is not null)
        {
            TemporalComparison comparison = PartialDate.Compare(end, start);
            if (comparison == TemporalComparison.Before)
            {
                throw new ArgumentException(
                    $"Period end '{end}' is definitely before start '{start}'.", nameof(end));
            }
        }

        return new PartialPeriod(start, end);
    }

    /// <summary>A period that starts at a known point and has no stated end (an ongoing period).</summary>
    public static PartialPeriod StartingAt(PartialDate start)
    {
        ArgumentNullException.ThrowIfNull(start);
        return new PartialPeriod(start, end: null);
    }

    /// <summary>A period that ends at a known point but whose start was not stated.</summary>
    public static PartialPeriod EndingAt(PartialDate end)
    {
        ArgumentNullException.ThrowIfNull(end);
        return new PartialPeriod(start: null, end);
    }

    /// <summary>A period with both boundaries stated.</summary>
    /// <exception cref="ArgumentException">The end is definitely before the start.</exception>
    public static PartialPeriod Between(PartialDate start, PartialDate end)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        return Create(start, end);
    }

    /// <summary>Representational equality on both boundaries.</summary>
    public bool Equals(PartialPeriod? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || (Start == other.Start && End == other.End);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as PartialPeriod);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Start, End);

    /// <summary>Representational equality. See <see cref="Equals(PartialPeriod?)"/>.</summary>
    public static bool operator ==(PartialPeriod? left, PartialPeriod? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Representational inequality. See <see cref="Equals(PartialPeriod?)"/>.</summary>
    public static bool operator !=(PartialPeriod? left, PartialPeriod? right) => !(left == right);

    /// <summary>
    /// Renders as <c>start/end</c>, using <c>..</c> for a boundary that was not stated, so an
    /// absent boundary is visible rather than implied.
    /// </summary>
    public override string ToString() => $"{Start?.ToString() ?? ".."}/{End?.ToString() ?? ".."}";
}
