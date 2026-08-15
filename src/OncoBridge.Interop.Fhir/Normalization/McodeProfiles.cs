using Hl7.Fhir.Model;

namespace OncoBridge.Interop.Fhir.Normalization;

internal static class McodeProfiles
{
    internal const string PrimaryCancerCondition =
        "http://hl7.org/fhir/us/mcode/StructureDefinition/mcode-primary-cancer-condition";

    private const char VersionSeparator = '|';

    internal static bool DeclaresPrimaryCancerCondition(Meta? meta) =>
        meta?.Profile?.Any(IsPrimaryCancerCondition) ?? false;

    private static bool IsPrimaryCancerCondition(string? profile)
    {
        if (profile is null)
        {
            return false;
        }

        int separator = profile.IndexOf(VersionSeparator);
        ReadOnlySpan<char> canonical = separator < 0 ? profile : profile.AsSpan(0, separator);

        return canonical.Equals(PrimaryCancerCondition, StringComparison.Ordinal);
    }
}
