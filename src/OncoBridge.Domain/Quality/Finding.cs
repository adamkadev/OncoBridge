namespace OncoBridge.Domain.Quality;

/// <summary>
/// One data-quality observation produced by a named check.
/// </summary>
/// <remarks>
/// <para>
/// The vocabulary is deliberately neutral. This type is a <c>Finding</c>, not an <c>Error</c>,
/// <c>Alert</c> or <c>Violation</c>, and it carries a <see cref="Message"/> rather than a
/// recommendation. OncoBridge reports what a check observed; it never tells anyone what to do about
/// it. That constraint is a control against medical-device language and is held in the type names
/// themselves, where it cannot be forgotten.
/// </para>
/// <para>
/// <b><see cref="Citation"/> is required, and it is not decoration.</b> Every check must trace to a
/// published specification statement. It makes each finding auditable in seconds, and it is the
/// evidence that these rules were derived from public standards rather than from anywhere else.
/// A check that cannot cite a source is not a check OncoBridge should run.
/// </para>
/// <para>
/// <see cref="Message"/> must be produced deterministically: the same input always yields the same
/// string, so findings can be compared across runs.
/// </para>
/// </remarks>
public sealed record Finding
{
    private Finding(
        CheckId checkId,
        FindingCategory category,
        FindingSeverity severity,
        string message,
        FindingTarget target,
        string citation,
        string? expected,
        string? actual)
    {
        CheckId = checkId;
        Category = category;
        Severity = severity;
        Message = message;
        Target = target;
        Citation = citation;
        Expected = expected;
        Actual = actual;
    }

    /// <summary>The check that produced this finding.</summary>
    public CheckId CheckId { get; }

    /// <summary>What kind of problem this is.</summary>
    public FindingCategory Category { get; }

    /// <summary>How serious it is, derived from the specification rather than chosen.</summary>
    public FindingSeverity Severity { get; }

    /// <summary>A deterministic description of what was observed.</summary>
    public string Message { get; }

    /// <summary>What this finding is attached to.</summary>
    public FindingTarget Target { get; }

    /// <summary>The published specification statement this check derives from.</summary>
    public string Citation { get; }

    /// <summary>What the specification expected, where that can be stated concisely.</summary>
    public string? Expected { get; }

    /// <summary>What was actually present, where that can be stated concisely.</summary>
    public string? Actual { get; }

    /// <summary>Creates a finding.</summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="message"/> or <paramref name="citation"/> is blank, or
    /// <paramref name="checkId"/> was left at its default value.
    /// </exception>
    public static Finding Create(
        CheckId checkId,
        FindingCategory category,
        FindingSeverity severity,
        string message,
        FindingTarget target,
        string citation,
        string? expected = null,
        string? actual = null)
    {
        if (checkId == default)
        {
            throw new ArgumentException(
                "A finding must name the check that produced it.", nameof(checkId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(citation);
        ArgumentNullException.ThrowIfNull(target);

        return new Finding(
            checkId, category, severity, message, target, citation, expected, actual);
    }

    /// <inheritdoc/>
    public override string ToString() => $"[{Severity}] {CheckId} on {Target}: {Message}";
}
