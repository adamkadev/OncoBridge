using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OncoBridge.Api.Contracts;
using OncoBridge.Api.Tests.Hosting;

namespace OncoBridge.Api.Tests.Architecture;

public sealed class ApiContractTests
{
    private const string Fhir = "Hl7.Fhir";

    private const string ContractsNamespace = "OncoBridge.Api.Contracts";

    private const string DomainAssembly = "OncoBridge.Domain";

    private const string ApplicationAssembly = "OncoBridge.Application";

    private static Assembly Api => typeof(ImportResponse).Assembly;

    [Fact]
    public void The_API_exposes_no_FHIR_type_through_its_public_surface()
    {
        string[] offenders =
        [
            .. Api.GetExportedTypes()
                .SelectMany(ContractGraph.SignatureTypesOf)
                .Select(ContractGraph.NameOf)
                .Where(name => name.StartsWith(Fhir, StringComparison.Ordinal)
                    || name.Contains($"[{Fhir}", StringComparison.Ordinal))
                .Distinct()
                .Order(),
        ];

        Assert.True(
            offenders.Length == 0,
            $"OncoBridge.Api must not expose {Fhir}.* through its public surface, but exposes: "
                + $"{string.Join(", ", offenders)}.");
    }

    [Fact]
    public void No_API_contract_carries_a_domain_or_application_type()
    {
        string[] offenders =
        [
            .. ContractTypes()
                .SelectMany(ContractGraph.PropertyTypesOf)
                .Where(type => type.Assembly.GetName().Name is DomainAssembly or ApplicationAssembly)
                .Select(ContractGraph.NameOf)
                .Distinct()
                .Order(),
        ];

        Assert.True(
            offenders.Length == 0,
            "The HTTP boundary owns its own contract, but an API contract carries: "
                + $"{string.Join(", ", offenders)}.");
    }

    [Fact]
    public async Task Every_declared_endpoint_response_type_is_an_API_contract()
    {
        string[] offenders =
        [
            .. (await ResponseTypesAsync())
                .Where(type => type.Namespace != ContractsNamespace)
                .Select(ContractGraph.NameOf)
                .Distinct()
                .Order(),
        ];

        Assert.True(
            offenders.Length == 0,
            $"Every endpoint response type must live in {ContractsNamespace}, but these do not: "
                + $"{string.Join(", ", offenders)}.");
    }

    [Fact]
    public async Task No_endpoint_declares_a_domain_entity_as_its_response_type()
    {
        Type[] forbidden =
        [
            typeof(Domain.Oncology.Patient),
            typeof(Domain.Oncology.PrimaryCancerDiagnosis),
            typeof(Domain.Oncology.CancerStaging),
            typeof(Domain.Oncology.CancerSurgicalProcedure),
            typeof(Domain.Quality.Finding),
            typeof(Domain.Provenance.Lineage),
            typeof(Domain.Provenance.ImportBatch),
            typeof(Domain.Provenance.SourceResource),
        ];

        Type[] declared = [.. await ResponseTypesAsync()];

        Assert.All(forbidden, type => Assert.DoesNotContain(type, declared));
    }

    [Fact]
    public async Task The_five_V1_routes_are_the_only_business_endpoints() =>
        Assert.Equal(
            [
                "GetDomainProvenance",
                "GetImport",
                "GetImportFindings",
                "GetPatientRecord",
                "ImportBundle",
            ],
            await OperationIdsAsync());

    private static async Task<string[]> OperationIdsAsync()
    {
        return
        [
            .. (await EndpointsAsync())
                .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
                .OfType<string>()
                .Distinct()
                .Order(StringComparer.Ordinal),
        ];
    }

    private static async Task<IReadOnlyList<Type>> ResponseTypesAsync() =>
        [
            .. (await EndpointsAsync())
                .SelectMany(endpoint => endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>())
                .Select(metadata => metadata.Type)
                .OfType<Type>()
                .Select(Unwrap)
                .Where(CarriesAnOncoBridgeBody),
        ];

    private static bool CarriesAnOncoBridgeBody(Type type) =>
        type != typeof(void) && type != typeof(Microsoft.AspNetCore.Mvc.ProblemDetails);

    private static async Task<IReadOnlyList<RouteEndpoint>> EndpointsAsync()
    {
        await using OncoBridgeApiFactory factory = OncoBridgeApiFactory.WithoutDatabase();
        using HttpClient client = factory.CreateClient();

        return
        [
            .. factory.Services.GetRequiredService<EndpointDataSource>()
                .Endpoints
                .OfType<RouteEndpoint>()
                .Where(endpoint =>
                    endpoint.RoutePattern.RawText?.StartsWith("/api/v1", StringComparison.Ordinal)
                        == true),
        ];
    }

    private static Type Unwrap(Type type) =>
        type.IsGenericType && type.GetGenericArguments().Length == 1
            ? Unwrap(type.GetGenericArguments()[0])
            : type;

    private static IEnumerable<Type> ContractTypes() =>
        Api.GetExportedTypes().Where(type => type.Namespace == ContractsNamespace);
}
