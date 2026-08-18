namespace OncoBridge.Api;

internal static class ApiMetadata
{
    internal const string DocumentName = "v1";

    internal const string Title = "OncoBridge API";

    internal const string Version = "v1";

    internal const string RoutePrefix = "/api/v1";

    internal const string Description =
        "Read-only views over imported FHIR R4 Bundles: the byte-exact import evidence, the "
        + "canonical oncology record derived from it, quality findings and field-level provenance. "
        + "Findings come from the OncoBridge conformance checks — a subset of mCODE STU4. "
        + "OncoBridge does not perform full mCODE profile validation.";
}
