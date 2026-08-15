namespace OncoBridge.Domain.Quality;

/// <summary>
/// A record that OncoBridge did not process something — which is not the same as finding
/// something wrong with it.
/// </summary>
/// <remarks>
/// <para>
/// A resource type outside V1 scope, or an occurrence stated in a form V1 does not read, produces
/// a coverage note. The data may be perfectly correct; OncoBridge simply did not look at it.
/// </para>
/// <para>
/// <b>This is a separate type from <see cref="Finding"/> on purpose, and it has no severity.</b>
/// Conflating "we did not examine this" with "this is wrong" is the most common failure mode of
/// data-quality tooling, and it undermines trust in every other number on the screen. Keeping the
/// two as distinct types with no shared base makes the conflation impossible rather than merely
/// discouraged: a coverage note cannot be counted among findings, because it will not compile as
/// one.
/// </para>
/// </remarks>
public sealed record CoverageNote
{
    private CoverageNote(string subject, string reason, FindingTarget? target)
    {
        Subject = subject;
        Reason = reason;
        Target = target;
    }

    /// <summary>What was not processed, e.g. a resource type or an element path.</summary>
    public string Subject { get; }

    /// <summary>Why it was not processed, stated as a scope fact rather than a judgement.</summary>
    public string Reason { get; }

    /// <summary>
    /// What the note relates to, where there is a specific target. Optional, because a note may
    /// describe a whole category of content rather than one resource.
    /// </summary>
    public FindingTarget? Target { get; }

    /// <summary>Creates a coverage note.</summary>
    /// <exception cref="ArgumentException"><paramref name="subject"/> or <paramref name="reason"/> is blank.</exception>
    public static CoverageNote Create(string subject, string reason, FindingTarget? target = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new CoverageNote(subject, reason, target);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        Target is null ? $"{Subject}: {Reason}" : $"{Subject} on {Target}: {Reason}";
}
