using System.Reflection;
using System.Text;

namespace OncoBridge.Infrastructure.Tests;

internal static class SyntheticFixtures
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    internal static byte[] MinimalBundleBytes { get; } =
        File.ReadAllBytes(Path.Combine(RepoRoot, "test-data/synthetic/phase2/bundle-minimal.json"));

    internal static byte[] CompleteNormalizationBundleBytes { get; } = File.ReadAllBytes(
        Path.Combine(RepoRoot, "test-data/synthetic/phase3/bundle-complete-normalization.json"));

    internal static byte[] Phase4Bundle(string name) =>
        File.ReadAllBytes(Path.Combine(RepoRoot, $"test-data/synthetic/phase4/{name}.json"));

    internal static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

    internal static byte[] MinimalBundleReserialisedCompactly()
    {
        using System.Text.Json.JsonDocument document =
            System.Text.Json.JsonDocument.Parse(MinimalBundleBytes);

        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(document.RootElement);
    }

    private static string ResolveRepoRoot()
    {
        string? value = typeof(SyntheticFixtures).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute => attribute.Key == "RepoRoot")
            ?.Value;

        return Path.GetFullPath(value ?? throw new InvalidOperationException("RepoRoot is not configured."));
    }
}
