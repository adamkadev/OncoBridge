using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Domain.Quality;

public sealed record FindingTarget
{
    private FindingTarget(FindingTargetKind kind, Guid id, string? domainEntityType)
    {
        Kind = kind;
        Id = id;
        DomainEntityType = domainEntityType;
    }

    public FindingTargetKind Kind { get; }

    public Guid Id { get; }

    public string? DomainEntityType { get; }

    public static FindingTarget ForSourceResource(SourceResourceId sourceResourceId) =>
        new(FindingTargetKind.SourceResource, sourceResourceId.Value, domainEntityType: null);

    public static FindingTarget ForDomainEntity(string domainEntityType, Guid domainEntityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainEntityType);
        return new FindingTarget(FindingTargetKind.DomainEntity, domainEntityId, domainEntityType);
    }

    public override string ToString() =>
        Kind == FindingTargetKind.SourceResource
            ? $"SourceResource/{Id}"
            : $"{DomainEntityType}/{Id}";
}
