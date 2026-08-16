using OncoBridge.Application.Normalization;
using OncoBridge.Domain.Identifiers;
using OncoBridge.Domain.Oncology;
using OncoBridge.Domain.Provenance;
using FhirCondition = Hl7.Fhir.Model.Condition;
using FhirPatient = Hl7.Fhir.Model.Patient;
using FhirProcedure = Hl7.Fhir.Model.Procedure;

namespace OncoBridge.Interop.Fhir.Normalization;

public sealed class FhirNormalizer : ICanonicalNormalizer
{
    private readonly FhirResourceReader _reader = new();

    public string Version => NormalizationMetadata.PipelineVersion;

    public NormalizationResult Normalize(IReadOnlyList<SourceResource> sourceResources)
    {
        ArgumentNullException.ThrowIfNull(sourceResources);

        SourceResourceReferenceIndex index = SourceResourceReferenceIndex.Build(sourceResources);
        HashSet<SourceResourceId> normalizedPatientSources = [];
        Dictionary<SourceResourceId, DiagnosisAssociation> diagnosedConditions = [];
        List<Patient> patients = [];
        List<PrimaryCancerDiagnosis> diagnoses = [];
        List<CancerStaging> stagings = [];
        List<CancerSurgicalProcedure> surgicalProcedures = [];
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
            diagnosedConditions[source.Id] =
                new DiagnosisAssociation(diagnosis.Id, patientId, patientSource.Id);
            lineage.Add(Lineage.ForEntity(
                NormalizationMetadata.PrimaryCancerDiagnosisEntityType,
                diagnosis.Id.Value,
                source.Id,
                NormalizationMetadata.PrimaryCancerDiagnosisTransformation,
                NormalizationMetadata.PrimaryCancerDiagnosisTransformationVersion));
        }

        foreach ((CancerStaging staging, SourceResourceId rootSourceId) in
            new CancerStagingMapper(_reader, index).Normalize(sourceResources, diagnosedConditions))
        {
            stagings.Add(staging);
            lineage.AddRange(StagingLineage(staging, rootSourceId));
        }

        foreach ((SourceResource source, FhirProcedure procedure) in
            EligibleCancerSurgicalProcedures(sourceResources))
        {
            if (index.Resolve(source.BatchId, procedure.Subject, FhirResourceTypes.Patient)
                is not { } patientSource)
            {
                continue;
            }

            if (NormalizePatient(patientSource, normalizedPatientSources, patients, lineage)
                is not { } patientId)
            {
                continue;
            }

            if (CancerSurgicalProcedureMapper.ToSurgicalProcedure(procedure, source.Id, patientId)
                is not { } surgicalProcedure)
            {
                continue;
            }

            surgicalProcedures.Add(surgicalProcedure);
            lineage.Add(Lineage.ForEntity(
                NormalizationMetadata.CancerSurgicalProcedureEntityType,
                surgicalProcedure.Id,
                source.Id,
                NormalizationMetadata.CancerSurgicalProcedureTransformation,
                NormalizationMetadata.CancerSurgicalProcedureTransformationVersion));
        }

        return new NormalizationResult
        {
            Patients = patients,
            PrimaryCancerDiagnoses = diagnoses,
            CancerStagings = stagings,
            CancerSurgicalProcedures = surgicalProcedures,
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

    private IEnumerable<(SourceResource Source, FhirProcedure Procedure)> EligibleCancerSurgicalProcedures(
        IEnumerable<SourceResource> sourceResources)
    {
        foreach (SourceResource source in sourceResources)
        {
            if (source.ResourceType != FhirResourceTypes.Procedure)
            {
                continue;
            }

            if (_reader.Read<FhirProcedure>(source) is { } procedure
                && McodeProfiles.DeclaresCancerRelatedSurgicalProcedure(procedure.Meta))
            {
                yield return (source, procedure);
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

    private static IEnumerable<Lineage> StagingLineage(
        CancerStaging staging, SourceResourceId rootSourceId)
    {
        yield return Lineage.ForEntity(
            NormalizationMetadata.CancerStagingEntityType,
            staging.Id,
            rootSourceId,
            NormalizationMetadata.CancerStagingTransformation,
            NormalizationMetadata.CancerStagingTransformationVersion);

        foreach (StageCategory category in staging.Categories)
        {
            yield return Lineage.ForField(
                NormalizationMetadata.CancerStagingEntityType,
                staging.Id,
                FieldPathOf(category.Axis),
                category.SourceResourceId,
                NormalizationMetadata.CancerStagingTransformation,
                NormalizationMetadata.CancerStagingTransformationVersion);
        }
    }

    private static string FieldPathOf(StageAxis axis) => axis switch
    {
        StageAxis.T => NormalizationMetadata.PrimaryTumourFieldPath,
        StageAxis.N => NormalizationMetadata.RegionalNodesFieldPath,
        StageAxis.M => NormalizationMetadata.DistantMetastasesFieldPath,
        _ => throw new InvalidOperationException($"Unhandled stage axis '{axis}'."),
    };
}
