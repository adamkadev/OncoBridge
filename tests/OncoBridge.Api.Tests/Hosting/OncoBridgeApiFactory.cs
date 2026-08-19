using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OncoBridge.Interop.Fhir.Ingestion;

namespace OncoBridge.Api.Tests.Hosting;

internal sealed class OncoBridgeApiFactory(
    string connectionString,
    string environment = OncoBridgeApiFactory.Development,
    BundleIngestionOptions? ingestionOptions = null)
    : WebApplicationFactory<Program>
{
    internal const string Development = "Development";

    internal const string Production = "Production";

    internal const string UnreachableConnectionString =
        "Host=oncobridge.invalid;Database=oncobridge_never_opened;Username=none;Password=none";

    private const string ConnectionStringKey = "ConnectionStrings:OncoBridge";

    internal static OncoBridgeApiFactory WithoutDatabase(string environment = Development) =>
        new(UnreachableConnectionString, environment);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { [ConnectionStringKey] = connectionString }));

        if (ingestionOptions is null)
        {
            return;
        }

        builder.ConfigureServices(services => services.Replace(
            ServiceDescriptor.Singleton(ingestionOptions)));
    }
}
