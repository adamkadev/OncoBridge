using System.Text.RegularExpressions;

namespace OncoBridge.Domain.Quality;

public readonly partial record struct CheckId
{
    private CheckId(string value) => Value = value;

    public string Value { get; }

    [GeneratedRegex(@"^OB-[A-Z]{2,6}-\d{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CheckIdPattern { get; }

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

    public override string ToString() => Value;
}
