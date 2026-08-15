namespace OncoBridge.Domain.Quality;

/// <summary>
/// What kind of problem a finding reports, ordered by the pipeline stage at which it becomes
/// detectable.
/// </summary>
/// <remarks>
/// The ordering matters: a category can only be evaluated once every category above it has passed.
/// A resource that does not parse can have nothing said about its conformance.
/// </remarks>
public enum FindingCategory
{
    /// <summary>The content could not be parsed. The resource never enters the domain.</summary>
    Structural,

    /// <summary>The content parses but does not meet an interoperability profile expectation.</summary>
    Conformance,

    /// <summary>A reference does not resolve, or resolves to something inconsistent.</summary>
    ReferentialIntegrity,

    /// <summary>The normalised result is internally incoherent.</summary>
    DomainConsistency,
}

/// <summary>
/// How serious a finding is.
/// </summary>
/// <remarks>
/// <para><b>Severity is derived, not chosen.</b> Assigning it by intuition is how a quality tool
/// becomes untrustworthy, so the rule is mechanical:</para>
/// <list type="bullet">
///   <item><description>
///     <see cref="Error"/> — the specification states the element is mandatory (minimum cardinality
///     of at least one), or the check is a pure structural or graph fact involving no interpretation.
///   </description></item>
///   <item><description>
///     <see cref="Warning"/> — the specification marks the element must-support rather than
///     mandatory, or the binding is extensible rather than required, or the finding is a
///     domain-consistency observation.
///   </description></item>
///   <item><description><see cref="Information"/> — context only.</description></item>
/// </list>
/// </remarks>
public enum FindingSeverity
{
    /// <summary>A mandatory expectation was not met, or a structural fact is definitely wrong.</summary>
    Error,

    /// <summary>An expectation that the specification does not make mandatory was not met.</summary>
    Warning,

    /// <summary>Context, neither a failure nor a warning.</summary>
    Information,
}

/// <summary>What a finding is attached to.</summary>
public enum FindingTargetKind
{
    /// <summary>A resource as received. Structural, conformance and referential findings attach here.</summary>
    SourceResource,

    /// <summary>A normalised entity. Domain-consistency findings attach here.</summary>
    DomainEntity,
}
