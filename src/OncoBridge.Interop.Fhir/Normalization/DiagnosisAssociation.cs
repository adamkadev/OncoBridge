using OncoBridge.Domain.Identifiers;

namespace OncoBridge.Interop.Fhir.Normalization;

internal readonly record struct DiagnosisAssociation(
    PrimaryCancerDiagnosisId DiagnosisId,
    PatientId PatientId,
    SourceResourceId PatientSourceResourceId);
