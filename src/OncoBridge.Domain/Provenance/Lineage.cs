using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Domain.Provenance;

public sealed class Lineage
{
    private Lineage()
    {
        DomainEntityType = string.Empty;
        TransformationName = string.Empty;
        TransformationVersion = string.Empty;
    }

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

    public string DomainEntityType { get; }

    public Guid DomainEntityId { get; }

    public string? FieldPath { get; }

    public SourceResourceId SourceResourceId { get; }

    public string TransformationName { get; }

    public string TransformationVersion { get; }

    public bool IsWholeEntity => FieldPath is null;

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
