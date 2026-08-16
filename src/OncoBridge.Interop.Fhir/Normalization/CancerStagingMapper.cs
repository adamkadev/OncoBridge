using Hl7.Fhir.Model;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Terminology;
using FhirObservation = Hl7.Fhir.Model.Observation;

namespace OncoBridge.Interop.Fhir.Normalization;

internal sealed class CancerStagingMapper
{
    private readonly FhirResourceReader _reader;

    private readonly SourceResourceReferenceIndex _index;

    internal CancerStagingMapper(FhirResourceReader reader, SourceResourceReferenceIndex index)
    {
        _reader = reader;
        _index = index;
    }

    internal IEnumerable<(CancerStaging Staging, SourceResourceId RootSourceId)> Normalize(
        IEnumerable<SourceResource> sourceResources,
        IReadOnlyDictionary<SourceResourceId, DiagnosisAssociation> diagnosedConditions)
    {
        foreach (SourceResource source in sourceResources)
        {
            if (source.ResourceType != FhirResourceTypes.Observation)
            {
                continue;
            }

            if (_reader.Read<FhirObservation>(source) is not { } observation
                || !TnmStagingCodes.IsStageGroup(observation.Code))
            {
                continue;
            }

            if (ToStaging(source, observation, diagnosedConditions) is { } staging)
            {
                yield return (staging, source.Id);
            }
        }
    }

    private CancerStaging? ToStaging(
        SourceResource source,
        FhirObservation observation,
        IReadOnlyDictionary<SourceResourceId, DiagnosisAssociation> diagnosedConditions)
    {
        if (ResolveFocus(source.BatchId, observation.Focus) is not { } conditionSource
            || !diagnosedConditions.TryGetValue(conditionSource.Id, out DiagnosisAssociation diagnosis)
            || ContradictsPatient(source.BatchId, observation.Subject, diagnosis))
        {
            return null;
        }

        if (ResolveCategories(source.BatchId, observation.HasMember, conditionSource, diagnosis)
            is not { } categories)
        {
            return null;
        }

        CodedConcept? stageGroup =
            FhirCodedConcepts.FromFirstUsableCoding(observation.Value as CodeableConcept);

        if (stageGroup is null && categories.Count == 0)
        {
            return null;
        }

        return new CancerStaging(
            source.Id.Value,
            diagnosis.PatientId,
            diagnosis.DiagnosisId,
            stageGroup,
            ToMethod(observation.Method),
            FhirTemporalMapper.ToPartialDate(observation.Effective),
            categories);
    }

    private SourceResource? ResolveFocus(ImportBatchId batchId, IEnumerable<ResourceReference>? focus)
    {
        SourceResource? resolved = null;

        foreach (ResourceReference reference in focus ?? [])
        {
            if (_index.Resolve(batchId, reference, FhirResourceTypes.Condition) is not { } candidate)
            {
                return null;
            }

            if (resolved is not null && resolved.Id != candidate.Id)
            {
                return null;
            }

            resolved = candidate;
        }

        return resolved;
    }

    private List<StageCategory>? ResolveCategories(
        ImportBatchId batchId,
        IEnumerable<ResourceReference>? hasMember,
        SourceResource conditionSource,
        DiagnosisAssociation diagnosis)
    {
        List<StageCategory> categories = [];
        HashSet<SourceResourceId> visited = [];

        foreach (ResourceReference reference in hasMember ?? [])
        {
            if (_index.Resolve(batchId, reference, FhirResourceTypes.Observation) is not { } memberSource
                || !visited.Add(memberSource.Id))
            {
                continue;
            }

            if (ToCategory(memberSource, conditionSource, diagnosis) is not { } category)
            {
                continue;
            }

            if (categories.Exists(existing => existing.Axis == category.Axis))
            {
                return null;
            }

            categories.Add(category);
        }

        return categories;
    }

    private StageCategory? ToCategory(
        SourceResource memberSource, SourceResource conditionSource, DiagnosisAssociation diagnosis)
    {
        if (_reader.Read<FhirObservation>(memberSource) is not { } member
            || TnmStagingCodes.AxisOf(member.Code) is not { } axis)
        {
            return null;
        }

        if (ContradictsCondition(memberSource.BatchId, member.Focus, conditionSource)
            || ContradictsPatient(memberSource.BatchId, member.Subject, diagnosis))
        {
            return null;
        }

        return FhirCodedConcepts.FromFirstUsableCoding(member.Value as CodeableConcept) is { } code
            ? new StageCategory(axis, code, memberSource.Id)
            : null;
    }

    private bool ContradictsPatient(
        ImportBatchId batchId, ResourceReference? subject, DiagnosisAssociation diagnosis) =>
        _index.Resolve(batchId, subject, FhirResourceTypes.Patient) is { } resolved
        && resolved.Id != diagnosis.PatientSourceResourceId;

    private bool ContradictsCondition(
        ImportBatchId batchId, IEnumerable<ResourceReference>? focus, SourceResource conditionSource)
    {
        foreach (ResourceReference reference in focus ?? [])
        {
            if (_index.Resolve(batchId, reference, FhirResourceTypes.Condition) is { } resolved
                && resolved.Id != conditionSource.Id)
            {
                return true;
            }
        }

        return false;
    }

    private static StagingMethod? ToMethod(CodeableConcept? method) =>
        FhirCodedConcepts.FromFirstUsableCoding(method) is { } code ? new StagingMethod(code) : null;
}
