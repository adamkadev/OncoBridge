namespace OncoBridge.Domain.Temporal;

/// <summary>
/// The precision at which a <see cref="PartialDate"/> was actually stated.
/// </summary>
/// <remarks>
/// Precision is recorded, never inferred and never widened. A value stated as
/// <c>2019</c> stays year-precision for its whole lifetime; it never silently becomes
/// <c>2019-01-01</c>. See ADR-0005.
/// </remarks>
public enum DatePrecision
{
    /// <summary>Year only, e.g. <c>2019</c>. No timezone.</summary>
    Year,

    /// <summary>Year and month, e.g. <c>2019-03</c>. No timezone.</summary>
    Month,

    /// <summary>Calendar day, e.g. <c>2019-03-14</c>. No timezone.</summary>
    Day,

    /// <summary>A point in time with a known UTC offset, e.g. <c>2019-03-14T10:00:00+02:00</c>.</summary>
    Instant,
}
