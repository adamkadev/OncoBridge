using System.Reflection;

namespace OncoBridge.Interop.Fhir.Tests;

/// <summary>
/// Asserts that Phase 1 did not begin the interoperability work.
/// </summary>
/// <remarks>
/// The project exists so the reference direction is fixed from the start, but it holds no code
/// until P3. This test makes that "not yet" explicit rather than assumed, and it is expected to be
/// <b>deleted as the first act of P3</b> — its failure is the signal that the phase has begun.
/// The assembly is loaded by name so that the project under test can stay genuinely empty.
/// </remarks>
public sealed class Phase1ScopeTests
{
    [Fact]
    public void Interop_Fhir_is_intentionally_empty_in_Phase_1()
    {
        Assembly interop = Assembly.Load(new AssemblyName("OncoBridge.Interop.Fhir"));

        Type[] publicTypes = interop.GetExportedTypes();

        Assert.True(
            publicTypes.Length == 0,
            "OncoBridge.Interop.Fhir holds no code until P3, but exposes: "
                + $"{string.Join(", ", publicTypes.Select(t => t.Name))}. "
                + "If P3 has started, delete this test as part of that phase.");
    }

    [Fact]
    public void Interop_Fhir_does_not_yet_reference_the_FHIR_SDK()
    {
        Assembly interop = Assembly.Load(new AssemblyName("OncoBridge.Interop.Fhir"));

        bool referencesFhir = interop
            .GetReferencedAssemblies()
            .Any(a => (a.Name ?? string.Empty).StartsWith("Hl7.Fhir", StringComparison.OrdinalIgnoreCase));

        Assert.False(referencesFhir, "Phase 1 gate item 7: no FHIR package is referenced yet.");
    }
}
