namespace OncoBridge.Domain.Quality;

public sealed record CoverageNote
{
    private CoverageNote(string subject, string reason, FindingTarget? target)
    {
        Subject = subject;
        Reason = reason;
        Target = target;
    }

    public string Subject { get; }

    public string Reason { get; }

    public FindingTarget? Target { get; }

    public static CoverageNote Create(string subject, string reason, FindingTarget? target = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new CoverageNote(subject, reason, target);
    }

    public override string ToString() =>
        Target is null ? $"{Subject}: {Reason}" : $"{Subject} on {Target}: {Reason}";
}
