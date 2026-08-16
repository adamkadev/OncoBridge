using Hl7.Fhir.Model;

namespace OncoBridge.Interop.Fhir.Normalization;

internal static class McodeProfiles
{
    internal const string PrimaryCancerCondition =
        "http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-primary-cancer-condition";

    internal const string CancerRelatedSurgicalProcedure =
        "http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-cancer-related-surgical-procedure";

    private const char VersionSeparator = '|';

    internal static bool DeclaresPrimaryCancerCondition(Meta? meta) =>
        DeclaresProfile(meta, PrimaryCancerCondition);

    internal static bool DeclaresCancerRelatedSurgicalProcedure(Meta? meta) =>
        DeclaresProfile(meta, CancerRelatedSurgicalProcedure);

    private static bool DeclaresProfile(Meta? meta, string canonical) =>
        meta?.Profile?.Any(profile => Declares(profile, canonical)) ?? false;

    private static bool Declares(string? profile, string canonical)
    {
        if (profile is null)
        {
            return false;
        }

        int separator = profile.IndexOf(VersionSeparator);
        ReadOnlySpan<char> declared = separator < 0 ? profile : profile.AsSpan(0, separator);

        return declared.Equals(canonical, StringComparison.Ordinal);
    }
}
