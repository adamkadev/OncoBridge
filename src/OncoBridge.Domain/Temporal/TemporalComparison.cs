namespace OncoBridge.Domain.Temporal;

/// <summary>
/// The outcome of comparing two temporal values that may be stated at different precisions.
/// </summary>
/// <remarks>
/// <para>
/// This is a <b>partial-order</b> comparison with an explicit indeterminate outcome, not a
/// total order. Variable-precision values genuinely cannot always be ordered: <c>2019</c>
/// and <c>2019-03</c> overlap, so neither precedes the other and they are not equivalent.
/// </para>
/// <para>
/// There are four outcomes. <see cref="Indeterminate"/> is a real answer, not an error case,
/// and callers must handle it explicitly rather than treating it as a default. No rule may
/// fabricate an ordering from it. See ADR-0005.
/// </para>
/// </remarks>
public enum TemporalComparison
{
    /// <summary>The first value's entire possible range precedes the second's.</summary>
    Before,

    /// <summary>The first value's entire possible range follows the second's.</summary>
    After,

    /// <summary>Both values denote exactly the same span of time.</summary>
    Same,

    /// <summary>
    /// The ranges overlap without being identical, so no ordering can be established.
    /// This is the correct answer, not a failure to compute one.
    /// </summary>
    Indeterminate,
}
