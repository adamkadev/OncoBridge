using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Domain.Quality;

/// <summary>
/// What a finding is attached to — either a resource as received or a normalised entity.
/// </summary>
/// <remarks>
/// <para>
/// The distinction is not filing convenience; it follows from what each finding is a statement
/// <i>about</i> (ADR-0004).
/// </para>
/// <list type="bullet">
///   <item><description>
///     "This resource lacks its mandatory method" is a claim about the <b>input</b>. It stays true
///     forever, whatever normalisation later does.
///   </description></item>
///   <item><description>
///     "This staging assessment predates its diagnosis" is a claim about <b>our normalised
///     result</b>, and could legitimately change when the mapper changes.
///   </description></item>
/// </list>
/// <para>
/// The practical payoff: re-running normalisation must invalidate domain-consistency findings and
/// must leave conformance findings untouched. That falls out cleanly from this split and is a mess
/// without it.
/// </para>
/// </remarks>
public sealed record FindingTarget
{
    private FindingTarget(FindingTargetKind kind, Guid id, string? domainEntityType)
    {
        Kind = kind;
        Id = id;
        DomainEntityType = domainEntityType;
    }

    /// <summary>Whether this target is a source resource or a domain entity.</summary>
    public FindingTargetKind Kind { get; }

    /// <summary>The identity of the target.</summary>
    public Guid Id { get; }

    /// <summary>
    /// The domain entity type, present only when <see cref="Kind"/> is
    /// <see cref="FindingTargetKind.DomainEntity"/>.
    /// </summary>
    public string? DomainEntityType { get; }

    /// <summary>Targets a resource as received.</summary>
    public static FindingTarget ForSourceResource(SourceResourceId sourceResourceId) =>
        new(FindingTargetKind.SourceResource, sourceResourceId.Value, domainEntityType: null);

    /// <summary>Targets a normalised entity.</summary>
    /// <exception cref="ArgumentException"><paramref name="domainEntityType"/> is blank.</exception>
    public static FindingTarget ForDomainEntity(string domainEntityType, Guid domainEntityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainEntityType);
        return new FindingTarget(FindingTargetKind.DomainEntity, domainEntityId, domainEntityType);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        Kind == FindingTargetKind.SourceResource
            ? $"SourceResource/{Id}"
            : $"{DomainEntityType}/{Id}";
}
