using System.Text.RegularExpressions;

namespace OncoBridge.Domain.Quality;

/// <summary>
/// The stable identifier of a data-quality check, e.g. <c>OB-CONF-002</c>.
/// </summary>
/// <remarks>
/// <para>
/// Format: <c>OB-{AREA}-{NNN}</c>, where <c>AREA</c> is two to six uppercase letters and
/// <c>NNN</c> is exactly three digits.
/// </para>
/// <para>
/// The identifier is deliberately opaque rather than descriptive. A name like
/// <c>StagingIncompleteWarning</c> reads as a clinical judgement about a patient's record;
/// <c>OB-CONF-002</c> reads as what it is, a reference to a documented check. That naming
/// discipline is a control against medical-device language, not a style preference.
/// </para>
/// <para>
/// Once published, an identifier is never renumbered or reused — findings recorded against it must
/// stay interpretable.
/// </para>
/// </remarks>
public readonly partial record struct CheckId
{
    private CheckId(string value) => Value = value;

    /// <summary>The identifier, e.g. <c>OB-CONF-002</c>.</summary>
    public string Value { get; }

    [GeneratedRegex(@"^OB-[A-Z]{2,6}-\d{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CheckIdPattern { get; }

    /// <summary>Parses a check identifier.</summary>
    /// <exception cref="ArgumentException">The value does not match <c>OB-{AREA}-{NNN}</c>.</exception>
    public static CheckId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!CheckIdPattern.IsMatch(value))
        {
            throw new ArgumentException(
                $"'{value}' is not a valid check id; expected the form OB-AREA-000.", nameof(value));
        }

        return new CheckId(value);
    }

    /// <summary>Attempts to parse a check identifier.</summary>
    public static bool TryParse(string? value, out CheckId checkId)
    {
        if (value is not null && CheckIdPattern.IsMatch(value))
        {
            checkId = new CheckId(value);
            return true;
        }

        checkId = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
