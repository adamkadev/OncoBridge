using Hl7.Fhir.Model;
using OncoBridge.Domain.Oncology;

namespace OncoBridge.Interop.Fhir.Normalization;

internal static class TnmStagingCodes
{
    private const string ClinicalStageGroup = "21908-9";

    private const string PathologicalStageGroup = "21902-2";

    private const string OtherStageGroup = "21914-7";

    private const string ClinicalPrimaryTumour = "21905-5";

    private const string PathologicalPrimaryTumour = "21899-0";

    private const string OtherPrimaryTumour = "21911-3";

    private const string ClinicalRegionalNodes = "21906-3";

    private const string PathologicalRegionalNodes = "21900-6";

    private const string OtherRegionalNodes = "21912-1";

    private const string ClinicalDistantMetastases = "21907-1";

    private const string PathologicalDistantMetastases = "21901-4";

    private const string OtherDistantMetastases = "21913-9";

    internal static bool IsStageGroup(CodeableConcept? concept)
    {
        foreach (string code in LoincCodesOf(concept))
        {
            if (code is ClinicalStageGroup or PathologicalStageGroup or OtherStageGroup)
            {
                return true;
            }
        }

        return false;
    }

    internal static StageAxis? AxisOf(CodeableConcept? concept)
    {
        foreach (string code in LoincCodesOf(concept))
        {
            if (AxisOfCode(code) is { } axis)
            {
                return axis;
            }
        }

        return null;
    }

    private static StageAxis? AxisOfCode(string code) => code switch
    {
        ClinicalPrimaryTumour or PathologicalPrimaryTumour or OtherPrimaryTumour => StageAxis.T,
        ClinicalRegionalNodes or PathologicalRegionalNodes or OtherRegionalNodes => StageAxis.N,
        ClinicalDistantMetastases or PathologicalDistantMetastases or OtherDistantMetastases => StageAxis.M,
        _ => null,
    };

    private static IEnumerable<string> LoincCodesOf(CodeableConcept? concept)
    {
        if (concept?.Coding is null)
        {
            yield break;
        }

        foreach (Coding coding in concept.Coding)
        {
            if (string.Equals(coding.System, CodeSystems.Loinc, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(coding.Code))
            {
                yield return coding.Code;
            }
        }
    }
}
