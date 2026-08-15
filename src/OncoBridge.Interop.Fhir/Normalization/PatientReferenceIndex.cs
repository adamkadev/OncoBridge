using Hl7.Fhir.Model;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Provenance;

namespace OncoBridge.Interop.Fhir.Normalization;

internal sealed class PatientReferenceIndex
{
    private const string RelativeReferencePrefix = FhirResourceTypes.Patient + "/";

    private readonly Dictionary<PatientReferenceKey, SourceResource> _resolvable;

    private readonly HashSet<PatientReferenceKey> _ambiguous;

    private PatientReferenceIndex(
        Dictionary<PatientReferenceKey, SourceResource> resolvable,
        HashSet<PatientReferenceKey> ambiguous)
    {
        _resolvable = resolvable;
        _ambiguous = ambiguous;
    }

    internal static PatientReferenceIndex Build(IEnumerable<SourceResource> sourceResources)
    {
        Dictionary<PatientReferenceKey, SourceResource> resolvable = [];
        HashSet<PatientReferenceKey> ambiguous = [];

        foreach (SourceResource source in sourceResources)
        {
            if (source.ResourceType != FhirResourceTypes.Patient)
            {
                continue;
            }

            foreach (string reference in ReferencesTo(source))
            {
                PatientReferenceKey key = new(source.BatchId, reference);

                if (!resolvable.TryAdd(key, source) && resolvable[key].Id != source.Id)
                {
                    ambiguous.Add(key);
                }
            }
        }

        return new PatientReferenceIndex(resolvable, ambiguous);
    }

    internal SourceResource? Resolve(ImportBatchId batchId, ResourceReference? subject)
    {
        if (string.IsNullOrWhiteSpace(subject?.Reference))
        {
            return null;
        }

        PatientReferenceKey key = new(batchId, subject.Reference);

        return _ambiguous.Contains(key) ? null : _resolvable.GetValueOrDefault(key);
    }

    private static IEnumerable<string> ReferencesTo(SourceResource source)
    {
        if (!string.IsNullOrWhiteSpace(source.FullUrl))
        {
            yield return source.FullUrl;
        }

        if (!string.IsNullOrWhiteSpace(source.SourceLogicalId))
        {
            yield return RelativeReferencePrefix + source.SourceLogicalId;
        }
    }

    private readonly record struct PatientReferenceKey(ImportBatchId BatchId, string Reference);
}
