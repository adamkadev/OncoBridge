namespace OncoBridge.Domain.Quality;

public enum FindingCategory
{
    Structural,

    Conformance,

    ReferentialIntegrity,

    DomainConsistency,
}

public enum FindingSeverity
{
    Error,

    Warning,

    Information,
}

public enum FindingTargetKind
{
    SourceResource,

    DomainEntity,
}
