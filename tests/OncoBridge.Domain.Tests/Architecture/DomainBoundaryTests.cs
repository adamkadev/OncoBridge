using System.Reflection;
using System.Xml.Linq;
using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Architecture;

/// <summary>
/// Makes the central architectural boundary executable (ADR-0001, ADR-0007).
/// </summary>
/// <remarks>
/// <para>
/// The whole project rests on one claim: the canonical domain is independent of the interchange
/// format. A boundary defended only by discipline is a boundary that has already been crossed, so
/// it is asserted here instead of in a code review.
/// </para>
/// <para>
/// <b>Two complementary checks, because either alone has a hole.</b> Reading the real
/// <c>.csproj</c> files catches a forbidden reference the moment it is declared, even before any
/// code uses it — which matters because the compiler omits unused references from assembly
/// metadata, so a declared-but-unused package would be invisible to reflection. Reading the loaded
/// assembly's reference graph catches anything that reaches the domain by a route the project file
/// does not spell out. Together they keep working once later phases actually add these packages.
/// </para>
/// </remarks>
public sealed class DomainBoundaryTests
{
    /// <summary>
    /// Packages that must never reach the domain. Matched case-insensitively as name prefixes.
    /// </summary>
    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Hl7.Fhir",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Microsoft.AspNetCore",
    ];

    private static string RepoRoot { get; } = ResolveRepoRoot();

    [Fact]
    public void Domain_project_declares_no_package_references()
    {
        XDocument project = LoadProject("src/OncoBridge.Domain/OncoBridge.Domain.csproj");

        string[] packages = project
            .Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value ?? "(unnamed)")
            .ToArray();

        Assert.True(
            packages.Length == 0,
            $"OncoBridge.Domain must have zero package references, but declares: "
                + $"{string.Join(", ", packages)}.");
    }

    [Fact]
    public void Domain_project_declares_no_project_references()
    {
        XDocument project = LoadProject("src/OncoBridge.Domain/OncoBridge.Domain.csproj");

        string[] references = project
            .Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? "(unnamed)")
            .ToArray();

        Assert.True(
            references.Length == 0,
            $"OncoBridge.Domain must sit at the bottom of the dependency graph, but references: "
                + $"{string.Join(", ", references)}.");
    }

    [Fact]
    public void Domain_assembly_does_not_reference_forbidden_frameworks()
    {
        Assembly domain = typeof(PartialDate).Assembly;

        string[] offenders = domain
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(IsForbidden)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"OncoBridge.Domain must not reference {string.Join(" / ", ForbiddenReferencePrefixes)}, "
                + $"but references: {string.Join(", ", offenders)}.");
    }

    /// <summary>
    /// Enforces the rule that <c>OncoBridge.Interop.Fhir</c> is the only production project allowed
    /// to reference the FHIR SDK.
    /// </summary>
    /// <remarks>
    /// This currently passes trivially, because no project references a FHIR package yet — that is
    /// itself Phase 1 gate item 7. It is written now so the constraint is already in force when P3
    /// adds the SDK, rather than being remembered afterwards.
    /// </remarks>
    [Fact]
    public void Only_Interop_Fhir_may_reference_the_FHIR_SDK()
    {
        List<string> offenders = [];

        foreach (string projectPath in ProductionProjects())
        {
            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            if (projectName == "OncoBridge.Interop.Fhir")
            {
                continue;
            }

            bool referencesFhir = XDocument.Load(projectPath)
                .Descendants("PackageReference")
                .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
                .Any(name => name.StartsWith("Hl7.Fhir", StringComparison.OrdinalIgnoreCase));

            if (referencesFhir)
            {
                offenders.Add(projectName);
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"Only OncoBridge.Interop.Fhir may reference Hl7.Fhir.*, but so do: "
                + $"{string.Join(", ", offenders)}.");
    }

    /// <summary>
    /// Phase 1 gate items 7 and 8: no FHIR, EF Core or Npgsql package is referenced anywhere yet.
    /// </summary>
    /// <remarks>
    /// Unlike the other tests here, this one is expected to be <i>deleted</i> — P2 adds EF Core and
    /// P3 adds the FHIR SDK, and this test should fail then. It exists so that Phase 1's "not yet"
    /// scope is asserted rather than assumed, and removing it is a deliberate act recorded in the
    /// phase that does so.
    /// </remarks>
    [Fact]
    public void Phase1_has_no_persistence_or_FHIR_packages_anywhere()
    {
        List<string> offenders = [];

        foreach (string projectPath in ProductionProjects())
        {
            IEnumerable<string> packages = XDocument.Load(projectPath)
                .Descendants("PackageReference")
                .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
                .Where(IsForbidden);

            offenders.AddRange(
                packages.Select(p => $"{Path.GetFileNameWithoutExtension(projectPath)} -> {p}"));
        }

        Assert.True(
            offenders.Count == 0,
            "Phase 1 declares no FHIR, EF Core or Npgsql packages in any production project, "
                + $"but found: {string.Join(", ", offenders)}. "
                + "If this failure is the intended start of P2 or P3, delete this test as part of that phase.");
    }

    private static bool IsForbidden(string name) =>
        ForbiddenReferencePrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> ProductionProjects() =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories);

    private static XDocument LoadProject(string relativePath)
    {
        string fullPath = Path.Combine(RepoRoot, relativePath);
        Assert.True(File.Exists(fullPath), $"Expected project file at '{fullPath}'.");
        return XDocument.Load(fullPath);
    }

    /// <summary>
    /// Resolves the repository root from an MSBuild-supplied assembly attribute rather than by
    /// walking up from the output directory, so the test behaves identically locally and in CI.
    /// </summary>
    private static string ResolveRepoRoot()
    {
        string? value = typeof(DomainBoundaryTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(a => a.Key == "RepoRoot")
            ?.Value;

        Assert.False(
            string.IsNullOrWhiteSpace(value),
            "The test project must supply a RepoRoot AssemblyMetadata item.");

        return Path.GetFullPath(value!);
    }
}
