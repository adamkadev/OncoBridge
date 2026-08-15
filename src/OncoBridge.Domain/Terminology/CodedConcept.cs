namespace OncoBridge.Domain.Terminology;

public sealed record CodedConcept(string System, string Code, string? Display = null)
{
    public string System { get; } = RequireNonBlank(System, nameof(System));

    public string Code { get; } = RequireNonBlank(Code, nameof(Code));

    private static string RequireNonBlank(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be blank.", parameterName);
        }

        return value;
    }

    public override string ToString() =>
        Display is null ? $"{System}|{Code}" : $"{System}|{Code} ({Display})";
}
