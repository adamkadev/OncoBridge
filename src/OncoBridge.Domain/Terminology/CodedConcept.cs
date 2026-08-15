namespace OncoBridge.Domain.Terminology;

/// <summary>
/// A code as supplied by the source, carried through unchanged.
/// </summary>
/// <remarks>
/// <para>
/// This type is the whole terminology boundary (ADR-0009). OncoBridge recognises code systems by
/// their URI and does nothing else with them: no value-set expansion, no subsumption, no concept
/// mapping, no translation between systems.
/// </para>
/// <para>
/// <b><see cref="Display"/> is only ever populated from the source.</b> OncoBridge never enriches a
/// display name from a table of its own. That is a licensing constraint as much as a design one —
/// SNOMED CT and LOINC content may not be redistributed in this repository, so the repository holds
/// code <i>references</i> and never terminology <i>content</i>.
/// </para>
/// <para>
/// Values are validated for presence but never altered: no trimming, no case folding, no
/// canonicalisation. "Carried as supplied" means exactly that.
/// </para>
/// </remarks>
/// <param name="System">The code system URI, e.g. <c>http://snomed.info/sct</c>.</param>
/// <param name="Code">The code within that system, exactly as supplied.</param>
/// <param name="Display">The human-readable display supplied by the source, if any.</param>
public sealed record CodedConcept(string System, string Code, string? Display = null)
{
    /// <summary>The code system URI, e.g. <c>http://snomed.info/sct</c>.</summary>
    public string System { get; } = RequireNonBlank(System, nameof(System));

    /// <summary>The code within that system, exactly as supplied.</summary>
    public string Code { get; } = RequireNonBlank(Code, nameof(Code));

    private static string RequireNonBlank(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be blank.", parameterName);
        }

        // Returned unchanged: presence is validated, content is never normalised.
        return value;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        Display is null ? $"{System}|{Code}" : $"{System}|{Code} ({Display})";
}
