using System.Reflection;
using System.Xml.Linq;
using OncoBridge.Domain.Temporal;

namespace OncoBridge.Domain.Tests.Architecture;

public sealed class DomainBoundaryTests
{
    private const string Fhir = "Hl7.Fhir";
    private const string EfCore = "Microsoft.EntityFrameworkCore";
    private const string Npgsql = "Npgsql";
    private const string AspNetCore = "Microsoft.AspNetCore";

    private static string RepoRoot { get; } = ResolveRepoRoot();

    public static TheoryData<string, string[]> ForbiddenProductionReferences => new()
    {
        { "OncoBridge.Domain", [Fhir, EfCore, Npgsql, AspNetCore] },
        { "OncoBridge.Application", [Fhir, EfCore, Npgsql] },
        { "OncoBridge.Interop.Fhir", [EfCore, Npgsql] },
        { "OncoBridge.Infrastructure", [Fhir] },
    };

    [Theory]
    [MemberData(nameof(ForbiddenProductionReferences))]
    public void A_production_project_declares_no_forbidden_package(string project, string[] forbidden)
    {
        string[] offenders =
        [
            .. PackageReferencesOf(project).Where(name => StartsWithAny(name, forbidden)),
        ];

        Assert.True(
            offenders.Length == 0,
            $"{project} must not reference {string.Join(" / ", forbidden)}, but declares: "
                + $"{string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Domain_declares_no_package_references_at_all()
    {
        string[] packages = [.. PackageReferencesOf("OncoBridge.Domain")];

        Assert.True(
            packages.Length == 0,
            $"OncoBridge.Domain must have zero package references, but declares: {string.Join(", ", packages)}.");
    }

    [Fact]
    public void Domain_declares_no_project_references_at_all()
    {
        string[] references =
        [
            .. LoadProject("OncoBridge.Domain")
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value ?? "(unnamed)"),
        ];

        Assert.True(
            references.Length == 0,
            $"OncoBridge.Domain must sit at the bottom of the dependency graph, but references: "
                + $"{string.Join(", ", references)}.");
    }

    [Fact]
    public void The_compiled_domain_assembly_references_nothing_forbidden()
    {
        string[] offenders =
        [
            .. typeof(PartialDate).Assembly
                .GetReferencedAssemblies()
                .Select(assembly => assembly.Name ?? string.Empty)
                .Where(name => StartsWithAny(name, [Fhir, EfCore, Npgsql, AspNetCore])),
        ];

        Assert.True(
            offenders.Length == 0,
            $"OncoBridge.Domain must not reference FHIR, EF Core, Npgsql or ASP.NET Core, "
                + $"but references: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Interop_Fhir_is_the_only_production_project_referencing_the_FHIR_SDK()
    {
        string[] offenders =
        [
            .. ProductionProjectNames()
                .Where(project => project != "OncoBridge.Interop.Fhir")
                .Where(project => PackageReferencesOf(project).Any(name => StartsWithAny(name, [Fhir]))),
        ];

        Assert.True(
            offenders.Length == 0,
            $"Only OncoBridge.Interop.Fhir may reference {Fhir}.*, but so do: {string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Infrastructure_is_the_only_production_project_referencing_EF_Core_or_Npgsql()
    {
        string[] offenders =
        [
            .. ProductionProjectNames()
                .Where(project => project != "OncoBridge.Infrastructure")
                .Where(project => PackageReferencesOf(project).Any(name => StartsWithAny(name, [EfCore, Npgsql]))),
        ];

        Assert.True(
            offenders.Length == 0,
            $"Only OncoBridge.Infrastructure may reference EF Core or Npgsql, but so do: "
                + $"{string.Join(", ", offenders)}.");
    }

    [Fact]
    public void Interop_Fhir_does_not_reference_Infrastructure() =>
        AssertDoesNotReferenceProject("OncoBridge.Interop.Fhir", "OncoBridge.Infrastructure");

    [Fact]
    public void Infrastructure_does_not_reference_Interop_Fhir() =>
        AssertDoesNotReferenceProject("OncoBridge.Infrastructure", "OncoBridge.Interop.Fhir");

    [Fact]
    public void Application_depends_on_the_domain_alone()
    {
        Assert.Equal(["OncoBridge.Domain"], ProjectReferencesOf("OncoBridge.Application"));
    }

    [Fact]
    public void The_adapters_depend_inward_on_the_application_that_owns_their_ports()
    {
        Assert.Contains("OncoBridge.Application", ProjectReferencesOf("OncoBridge.Interop.Fhir"));
        Assert.Contains("OncoBridge.Application", ProjectReferencesOf("OncoBridge.Infrastructure"));
    }

    private static void AssertDoesNotReferenceProject(string project, string forbidden)
    {
        Assert.True(
            !ProjectReferencesOf(project).Contains(forbidden),
            $"{project} must not reference {forbidden}.");
    }

    private static string[] ProjectReferencesOf(string project) =>
    [
        .. LoadProject(project)
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Select(include => Path.GetFileNameWithoutExtension(include))
            .Order(),
    ];

    [Fact]
    public void Api_has_not_started_and_references_no_web_packages()
    {
        string[] offenders =
        [
            .. PackageReferencesOf("OncoBridge.Api").Where(name => StartsWithAny(name, [AspNetCore])),
        ];

        Assert.True(
            offenders.Length == 0,
            $"OncoBridge.Api holds no implementation until P5, but declares: {string.Join(", ", offenders)}.");
    }

    private static bool StartsWithAny(string name, IEnumerable<string> prefixes) =>
        prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> PackageReferencesOf(string project) =>
        LoadProject(project)
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? "(unnamed)");

    private static IEnumerable<string> ProductionProjectNames() =>
        Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>();

    private static XDocument LoadProject(string project)
    {
        string path = Path.Combine(RepoRoot, "src", project, $"{project}.csproj");
        Assert.True(File.Exists(path), $"Expected project file at '{path}'.");
        return XDocument.Load(path);
    }

    private static string ResolveRepoRoot()
    {
        string? value = typeof(DomainBoundaryTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute => attribute.Key == "RepoRoot")
            ?.Value;

        Assert.False(
            string.IsNullOrWhiteSpace(value),
            "The test project must supply a RepoRoot AssemblyMetadata item.");

        return Path.GetFullPath(value!);
    }
}
