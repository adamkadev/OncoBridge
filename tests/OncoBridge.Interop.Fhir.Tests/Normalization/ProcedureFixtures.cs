using OncoBridge.Domain.Oncology;
using OncoBridge.Interop.Fhir.Normalization;

namespace OncoBridge.Interop.Fhir.Tests.Normalization;

internal static class ProcedureFixtures
{
    internal const string PatientLogicalId = "patient-001";

    internal const string ProcedureLogicalId = "procedure-001";

    internal const string LumpectomySnomedCode = "392021009";

    internal const string BreastBodySite =
        """ "bodySite":[{"coding":[{"system":"http://snomed.info/sct","code":"76752008"}]}] """;

    internal static string PatientEntry() =>
        NormalizationFixtures.PatientEntry(NormalizationFixtures.PatientFullUrl, PatientLogicalId);

    internal static string SurgicalProcedureEntry(params string[] members) =>
        NormalizationFixtures.SurgicalProcedureEntry(
            NormalizationFixtures.ProcedureFullUrl,
            ProcedureLogicalId,
            NormalizationFixtures.PatientFullUrl,
            members);

    internal static NormalizationResult NormalizeProcedureStating(params string[] members) =>
        NormalizationFixtures.NormalizeEntries(PatientEntry(), SurgicalProcedureEntry(members));

    internal static CancerSurgicalProcedure NormalizeProcedureWith(params string[] members) =>
        Assert.Single(
            NormalizeProcedureStating([NormalizationFixtures.LumpectomyCode, .. members])
                .CancerSurgicalProcedures);
}
