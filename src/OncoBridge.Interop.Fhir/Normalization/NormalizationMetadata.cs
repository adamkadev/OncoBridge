namespace OncoBridge.Interop.Fhir.Normalization;

internal static class NormalizationMetadata
{
    internal const string PipelineVersion = "1.0.0";

    internal const string PatientEntityType = "Patient";

    internal const string PatientTransformation = "FhirPatientNormalization";

    internal const string PatientTransformationVersion = "1.0.0";

    internal const string PrimaryCancerDiagnosisEntityType = "PrimaryCancerDiagnosis";

    internal const string PrimaryCancerDiagnosisTransformation = "FhirPrimaryCancerDiagnosisNormalization";

    internal const string PrimaryCancerDiagnosisTransformationVersion = "1.0.0";

    internal const string CancerStagingEntityType = "CancerStaging";

    internal const string CancerStagingTransformation = "FhirCancerStagingNormalization";

    internal const string CancerStagingTransformationVersion = "1.0.0";

    internal const string CancerSurgicalProcedureEntityType = "CancerSurgicalProcedure";

    internal const string CancerSurgicalProcedureTransformation =
        "FhirCancerSurgicalProcedureNormalization";

    internal const string CancerSurgicalProcedureTransformationVersion = "1.0.0";

    internal const string PrimaryTumourFieldPath = "PrimaryTumour";

    internal const string RegionalNodesFieldPath = "RegionalNodes";

    internal const string DistantMetastasesFieldPath = "DistantMetastases";
}
