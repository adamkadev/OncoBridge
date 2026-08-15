namespace OncoBridge.Domain.Quality;

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

    public CheckId CheckId { get; }

    public FindingCategory Category { get; }

    public FindingSeverity Severity { get; }

    public string Message { get; }

    public FindingTarget Target { get; }

    public string Citation { get; }

    public string? Expected { get; }

    public string? Actual { get; }

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

    public override string ToString() => $"[{Severity}] {CheckId} on {Target}: {Message}";
}
