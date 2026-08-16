using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using FhirCondition = Hl7.Fhir.Model.Condition;
using FhirPatient = Hl7.Fhir.Model.Patient;

namespace OncoBridge.Interop.Fhir.Normalization;

public sealed class FhirNormalizer
{
    private readonly FhirResourceReader _reader = new();

    public NormalizationResult Normalize(IReadOnlyList<SourceResource> sourceResources)
    {
        ArgumentNullException.ThrowIfNull(sourceResources);

        SourceResourceReferenceIndex index = SourceResourceReferenceIndex.Build(sourceResources);
        HashSet<SourceResourceId> normalizedPatientSources = [];
        List<Patient> patients = [];
        List<PrimaryCancerDiagnosis> diagnoses = [];
        List<Lineage> lineage = [];

        foreach ((SourceResource source, FhirCondition condition) in
            EligiblePrimaryCancerConditions(sourceResources))
        {
            if (index.Resolve(source.BatchId, condition.Subject, FhirResourceTypes.Patient)
                is not { } patientSource)
            {
                continue;
            }

            if (NormalizePatient(patientSource, normalizedPatientSources, patients, lineage)
                is not { } patientId)
            {
                continue;
            }

            if (PrimaryCancerDiagnosisMapper.ToDiagnosis(condition, source.Id, patientId)
                is not { } diagnosis)
            {
                continue;
            }

            diagnoses.Add(diagnosis);
            lineage.Add(Lineage.ForEntity(
                NormalizationMetadata.PrimaryCancerDiagnosisEntityType,
                diagnosis.Id,
                source.Id,
                NormalizationMetadata.PrimaryCancerDiagnosisTransformation,
                NormalizationMetadata.PrimaryCancerDiagnosisTransformationVersion));
        }

        return new NormalizationResult
        {
            Patients = patients,
            PrimaryCancerDiagnoses = diagnoses,
            Lineage = lineage,
        };
    }

    private IEnumerable<(SourceResource Source, FhirCondition Condition)> EligiblePrimaryCancerConditions(
        IEnumerable<SourceResource> sourceResources)
    {
        foreach (SourceResource source in sourceResources)
        {
            if (source.ResourceType != FhirResourceTypes.Condition)
            {
                continue;
            }

            if (_reader.Read<FhirCondition>(source) is { } condition
                && McodeProfiles.DeclaresPrimaryCancerCondition(condition.Meta))
            {
                yield return (source, condition);
            }
        }
    }

    private PatientId? NormalizePatient(
        SourceResource patientSource,
        HashSet<SourceResourceId> normalizedPatientSources,
        List<Patient> patients,
        List<Lineage> lineage)
    {
        PatientId patientId = new(patientSource.Id.Value);

        if (normalizedPatientSources.Contains(patientSource.Id))
        {
            return patientId;
        }

        if (_reader.Read<FhirPatient>(patientSource) is not { } source)
        {
            return null;
        }

        Patient patient = FhirPatientMapper.ToPatient(source, patientId);

        normalizedPatientSources.Add(patientSource.Id);
        patients.Add(patient);
        lineage.Add(Lineage.ForEntity(
            NormalizationMetadata.PatientEntityType,
            patient.Id.Value,
            patientSource.Id,
            NormalizationMetadata.PatientTransformation,
            NormalizationMetadata.PatientTransformationVersion));

        return patient.Id;
    }
}
