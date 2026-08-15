using System.Reflection;
using System.Text;

namespace OncoBridge.Interop.Fhir.Tests;

internal static class SyntheticFixtures
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    internal static byte[] MinimalBundleBytes { get; } =
        File.ReadAllBytes(Path.Combine(RepoRoot, "test-data/synthetic/phase2/bundle-minimal.json"));

    internal static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

    private static string ResolveRepoRoot()
    {
        string? value = typeof(SyntheticFixtures).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute => attribute.Key == "RepoRoot")
            ?.Value;

        return Path.GetFullPath(value ?? throw new InvalidOperationException("RepoRoot is not configured."));
    }
}
