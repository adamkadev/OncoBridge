using Hl7.Fhir.Model;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Interop.Fhir.Normalization;

internal sealed class SourceResourceReferenceIndex
{
    private const char RelativeReferenceSeparator = '/';

    private readonly Dictionary<ReferenceKey, SourceResource> _resolvable;

    private readonly HashSet<ReferenceKey> _ambiguous;

    private SourceResourceReferenceIndex(
        Dictionary<ReferenceKey, SourceResource> resolvable,
        HashSet<ReferenceKey> ambiguous)
    {
        _resolvable = resolvable;
        _ambiguous = ambiguous;
    }

    internal static SourceResourceReferenceIndex Build(IEnumerable<SourceResource> sourceResources)
    {
        Dictionary<ReferenceKey, SourceResource> resolvable = [];
        HashSet<ReferenceKey> ambiguous = [];

        foreach (SourceResource source in sourceResources)
        {
            if (string.IsNullOrWhiteSpace(source.ResourceType))
            {
                continue;
            }

            foreach (string reference in ReferencesTo(source))
            {
                ReferenceKey key = new(source.BatchId, reference);

                if (!resolvable.TryAdd(key, source) && resolvable[key].Id != source.Id)
                {
                    ambiguous.Add(key);
                }
            }
        }

        return new SourceResourceReferenceIndex(resolvable, ambiguous);
    }

    internal SourceResource? Resolve(
        ImportBatchId batchId, ResourceReference? reference, string resourceType)
    {
        if (string.IsNullOrWhiteSpace(reference?.Reference))
        {
            return null;
        }

        ReferenceKey key = new(batchId, reference.Reference);

        if (_ambiguous.Contains(key))
        {
            return null;
        }

        SourceResource? source = _resolvable.GetValueOrDefault(key);

        return source?.ResourceType == resourceType ? source : null;
    }

    private static IEnumerable<string> ReferencesTo(SourceResource source)
    {
        if (!string.IsNullOrWhiteSpace(source.FullUrl))
        {
            yield return source.FullUrl;
        }

        if (!string.IsNullOrWhiteSpace(source.SourceLogicalId))
        {
            yield return source.ResourceType + RelativeReferenceSeparator + source.SourceLogicalId;
        }
    }

    private readonly record struct ReferenceKey(ImportBatchId BatchId, string Reference);
}
