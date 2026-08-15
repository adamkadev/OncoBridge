using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Domain.Provenance;

/// <summary>
/// A record that a named transformation produced part or all of a domain entity from a particular
/// source resource.
/// </summary>
/// <remarks>
/// <para>
/// <b>Granularity is deliberate.</b> The default is entity-level lineage:
/// <see cref="FieldPath"/> is <see langword="null"/>, meaning "this entity, wholly, from this
/// source". Field-level records are emitted only where an entity genuinely draws from more than one
/// source resource — in V1 that is <c>CancerStaging</c> alone, which takes its stage group from one
/// resource and each axis category from another.
/// </para>
/// <para>
/// Recording a field-level row for every property would multiply storage, couple every mapper to a
/// lineage API, and produce a display nobody reads. Recording it only where several sources
/// converge means every field-level row present actually carries information.
/// </para>
/// <para>
/// <see cref="TransformationVersion"/> is stored alongside the name so that lineage recorded by an
/// older mapper stays interpretable after the mapper changes.
/// </para>
/// </remarks>
public sealed class Lineage
{
    private Lineage(
        string domainEntityType,
        Guid domainEntityId,
        string? fieldPath,
        SourceResourceId sourceResourceId,
        string transformationName,
        string transformationVersion)
    {
        DomainEntityType = domainEntityType;
        DomainEntityId = domainEntityId;
        FieldPath = fieldPath;
        SourceResourceId = sourceResourceId;
        TransformationName = transformationName;
        TransformationVersion = transformationVersion;
    }

    /// <summary>The domain entity type this lineage describes, e.g. <c>CancerStaging</c>.</summary>
    public string DomainEntityType { get; }

    /// <summary>The identity of the domain entity this lineage describes.</summary>
    public Guid DomainEntityId { get; }

    /// <summary>
    /// The specific field this record covers, or <see langword="null"/> when the record covers the
    /// whole entity.
    /// </summary>
    public string? FieldPath { get; }

    /// <summary>The source resource the value was drawn from.</summary>
    public SourceResourceId SourceResourceId { get; }

    /// <summary>The name of the transformation that produced the value.</summary>
    public string TransformationName { get; }

    /// <summary>The version of that transformation.</summary>
    public string TransformationVersion { get; }

    /// <summary>Whether this record covers the whole entity rather than one field.</summary>
    public bool IsWholeEntity => FieldPath is null;

    /// <summary>Records that a whole entity was produced from a single source resource.</summary>
    /// <exception cref="ArgumentException">A required string argument is blank.</exception>
    public static Lineage ForEntity(
        string domainEntityType,
        Guid domainEntityId,
        SourceResourceId sourceResourceId,
        string transformationName,
        string transformationVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainEntityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(transformationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(transformationVersion);

        return new Lineage(
            domainEntityType,
            domainEntityId,
            fieldPath: null,
            sourceResourceId,
            transformationName,
            transformationVersion);
    }

    /// <summary>
    /// Records that one field of an entity was produced from a particular source resource. Use
    /// only where an entity draws from several sources.
    /// </summary>
    /// <exception cref="ArgumentException">A required string argument is blank.</exception>
    public static Lineage ForField(
        string domainEntityType,
        Guid domainEntityId,
        string fieldPath,
        SourceResourceId sourceResourceId,
        string transformationName,
        string transformationVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainEntityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(transformationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(transformationVersion);

        return new Lineage(
            domainEntityType,
            domainEntityId,
            fieldPath,
            sourceResourceId,
            transformationName,
            transformationVersion);
    }
}
