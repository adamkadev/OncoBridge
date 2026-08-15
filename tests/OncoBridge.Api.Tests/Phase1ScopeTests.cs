using System.Reflection;

namespace OncoBridge.Api.Tests;

public sealed class Phase1ScopeTests
{
    [Fact]
    public void Api_is_intentionally_empty_in_Phase_1()
    {
        Assembly api = Assembly.Load(new AssemblyName("OncoBridge.Api"));

        Type[] publicTypes = api.GetExportedTypes();

        Assert.True(
            publicTypes.Length == 0,
            "OncoBridge.Api holds no code until P5, but exposes: "
                + $"{string.Join(", ", publicTypes.Select(t => t.Name))}. "
                + "If P5 has started, delete this test as part of that phase.");
    }

    [Fact]
    public void Api_does_not_yet_reference_ASP_NET_Core()
    {
        Assembly api = Assembly.Load(new AssemblyName("OncoBridge.Api"));

        bool referencesAspNet = api
            .GetReferencedAssemblies()
            .Any(a => (a.Name ?? string.Empty)
                .StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));

        Assert.False(referencesAspNet, "Phase 1 gate item 9: the API has not started.");
    }
}
