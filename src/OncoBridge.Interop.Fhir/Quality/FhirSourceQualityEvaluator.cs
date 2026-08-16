using Hl7.Fhir.Model;
using OncoBridge.Application.Quality;
using OncoBridge.Domain.Provenance;
using OncoBridge.Domain.Quality;
using OncoBridge.Interop.Fhir.Normalization;
using FhirCondition = Hl7.Fhir.Model.Condition;
using FhirObservation = Hl7.Fhir.Model.Observation;
using FhirPatient = Hl7.Fhir.Model.Patient;
using FhirProcedure = Hl7.Fhir.Model.Procedure;

namespace OncoBridge.Interop.Fhir.Quality;

public sealed class FhirSourceQualityEvaluator : ISourceQualityEvaluator
{
    private const string ProblemListItem = "problem-list-item";

    private const string HealthConcern = "health-concern";

    private const char ContainedReferenceMarker = '#';

    private const string ConditionSubject = "Condition.subject";

    private const string ObservationSubject = "Observation.subject";

    private const string ObservationFocus = "Observation.focus";

    private const string ObservationHasMember = "Observation.hasMember";

    private const string ProcedureSubject = "Procedure.subject";

    private const string ProcedureReasonReference = "Procedure.reasonReference";

    private readonly FhirResourceReader _reader = new();

    public SourceQualityAssessment Assess(IReadOnlyList<SourceResource> sourceResources)
    {
        ArgumentNullException.ThrowIfNull(sourceResources);

        SourceResourceReferenceIndex index = SourceResourceReferenceIndex.Build(sourceResources);
        List<Finding> findings = [];
        List<CoverageNote> notes = [];

        foreach (SourceResource source in sourceResources.OrderBy(resource => resource.EntryIndex))
        {
            if (_reader.Read(source) is not { } resource)
            {
                findings.Add(SourceQualityFindings.UnparseableEntry(source));
                continue;
            }

            Assess(source, resource, index, findings, notes);
        }

        return new SourceQualityAssessment { Findings = findings, CoverageNotes = notes };
    }

    private void Assess(
        SourceResource source,
        Resource resource,
        SourceResourceReferenceIndex index,
        List<Finding> findings,
        List<CoverageNote> notes)
    {
        switch (resource)
        {
            case FhirPatient:
                break;

            case FhirCondition condition:
                AssessCondition(source, condition, index, findings, notes);
                break;

            case FhirObservation observation:
                AssessObservation(source, observation, index, findings, notes);
                break;

            case FhirProcedure procedure:
                AssessProcedure(source, procedure, index, findings, notes);
                break;

            default:
                notes.Add(
                    SourceQualityCoverage.ResourceTypeOutsideCoverage(source, resource.TypeName));
                break;
        }
    }

    private void AssessCondition(
        SourceResource source,
        FhirCondition condition,
        SourceResourceReferenceIndex index,
        List<Finding> findings,
        List<CoverageNote> notes)
    {
        if (!McodeProfiles.DeclaresPrimaryCancerCondition(condition.Meta))
        {
            return;
        }

        if (!StatesRequiredCategory(condition))
        {
            findings.Add(SourceQualityFindings.MissingPrimaryCancerConditionCategory(
                source, DescribeCategories(condition)));
        }

        AssessReference(source, condition, condition.Subject, ConditionSubject, index, findings, notes);

        if (UnreadOccurrenceTypeOf(condition.Onset) is { } stated)
        {
            notes.Add(
                SourceQualityCoverage.UnreadOccurrenceForm(source, "Condition.onset[x]", stated));
        }
    }

    private void AssessObservation(
        SourceResource source,
        FhirObservation observation,
        SourceResourceReferenceIndex index,
        List<Finding> findings,
        List<CoverageNote> notes)
    {
        if (TnmStagingCodes.IsStageGroup(observation.Code))
        {
            if (observation.Method is null)
            {
                findings.Add(SourceQualityFindings.MissingStageGroupMethod(source));
            }

            AssessReference(
                source, observation, observation.Subject, ObservationSubject, index, findings, notes);
            AssessReferences(
                source, observation, observation.Focus, ObservationFocus, index, findings, notes);
            AssessReferences(
                source, observation, observation.HasMember, ObservationHasMember, index, findings, notes);
            AssessMemberSubjects(source, observation, index, findings);

            return;
        }

        if (TnmStagingCodes.AxisOf(observation.Code) is not null)
        {
            AssessReference(
                source, observation, observation.Subject, ObservationSubject, index, findings, notes);
            AssessReferences(
                source, observation, observation.Focus, ObservationFocus, index, findings, notes);
        }
    }

    private void AssessProcedure(
        SourceResource source,
        FhirProcedure procedure,
        SourceResourceReferenceIndex index,
        List<Finding> findings,
        List<CoverageNote> notes)
    {
        if (!McodeProfiles.DeclaresCancerRelatedSurgicalProcedure(procedure.Meta))
        {
            return;
        }

        AssessReference(source, procedure, procedure.Subject, ProcedureSubject, index, findings, notes);
        AssessReferences(
            source, procedure, procedure.ReasonReference, ProcedureReasonReference, index, findings, notes);

        if (UnreadOccurrenceTypeOf(procedure.Performed) is { } stated)
        {
            notes.Add(
                SourceQualityCoverage.UnreadOccurrenceForm(source, "Procedure.performed[x]", stated));
        }
    }

    private void AssessMemberSubjects(
        SourceResource source,
        FhirObservation group,
        SourceResourceReferenceIndex index,
        List<Finding> findings)
    {
        if (index.Resolve(source.BatchId, group.Subject, FhirResourceTypes.Patient)
            is not { } groupPatient)
        {
            return;
        }

        foreach (ResourceReference reference in group.HasMember ?? [])
        {
            if (index.Resolve(source.BatchId, reference, FhirResourceTypes.Observation)
                    is not { } memberSource
                || _reader.Read<FhirObservation>(memberSource) is not { } member
                || TnmStagingCodes.AxisOf(member.Code) is null)
            {
                continue;
            }

            if (index.Resolve(source.BatchId, member.Subject, FhirResourceTypes.Patient)
                    is not { } memberPatient
                || memberPatient.Id == groupPatient.Id)
            {
                continue;
            }

            findings.Add(SourceQualityFindings.StageGroupSubjectDisagreement(
                source,
                reference.Reference!,
                group.Subject!.Reference!,
                member.Subject!.Reference!));
        }
    }

    private static void AssessReferences(
        SourceResource source,
        Resource resource,
        IEnumerable<ResourceReference>? references,
        string fieldPath,
        SourceResourceReferenceIndex index,
        List<Finding> findings,
        List<CoverageNote> notes)
    {
        int position = 0;

        foreach (ResourceReference reference in references ?? [])
        {
            AssessReference(
                source, resource, reference, $"{fieldPath}[{position}]", index, findings, notes);
            position++;
        }
    }

    private static void AssessReference(
        SourceResource source,
        Resource resource,
        ResourceReference? reference,
        string fieldPath,
        SourceResourceReferenceIndex index,
        List<Finding> findings,
        List<CoverageNote> notes)
    {
        if (reference is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(reference.Reference))
        {
            if (reference.Identifier is not null)
            {
                notes.Add(SourceQualityCoverage.IdentifierOnlyReference(source, fieldPath));
            }

            return;
        }

        bool resolved = reference.Reference[0] == ContainedReferenceMarker
            ? ResolvesToContained(resource, reference.Reference)
            : index.Resolve(source.BatchId, reference) is not null;

        if (!resolved)
        {
            findings.Add(
                SourceQualityFindings.UnresolvedReference(source, fieldPath, reference.Reference));
        }
    }

    private static bool ResolvesToContained(Resource resource, string reference)
    {
        string id = reference[1..];

        return resource is DomainResource domain
            && domain.Contained.Count(contained =>
                string.Equals(contained.Id, id, StringComparison.Ordinal)) == 1;
    }

    private static bool StatesRequiredCategory(FhirCondition condition)
    {
        foreach (CodeableConcept category in condition.Category ?? [])
        {
            foreach (Coding coding in category.Coding ?? [])
            {
                if (IsRequiredCategory(coding))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsRequiredCategory(Coding coding) =>
        (string.Equals(coding.System, CodeSystems.ConditionCategory, StringComparison.Ordinal)
            && string.Equals(coding.Code, ProblemListItem, StringComparison.Ordinal))
        || (string.Equals(coding.System, CodeSystems.UsCoreConditionCategory, StringComparison.Ordinal)
            && string.Equals(coding.Code, HealthConcern, StringComparison.Ordinal));

    private static string DescribeCategories(FhirCondition condition)
    {
        string[] stated =
        [
            .. (condition.Category ?? [])
                .SelectMany(category => category.Coding ?? [])
                .Select(coding => $"{coding.System}|{coding.Code}"),
        ];

        return stated.Length == 0
            ? "no Condition.category coding is stated"
            : $"stated Condition.category codings: {string.Join(", ", stated)}";
    }

    private static string? UnreadOccurrenceTypeOf(DataType? occurrence) => occurrence switch
    {
        null or FhirDateTime or Period => null,
        _ => occurrence.TypeName,
    };
}
