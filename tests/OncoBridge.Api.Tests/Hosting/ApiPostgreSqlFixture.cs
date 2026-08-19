using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OncoBridge.Infrastructure.Persistence;
using OncoBridge.Interop.Fhir.Ingestion;
using Testcontainers.PostgreSql;

namespace OncoBridge.Api.Tests.Hosting;

public sealed class ApiPostgreSqlFixture : IAsyncLifetime
{
    private const string PostgreSqlImage = "postgres:18.6";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(PostgreSqlImage)
        .WithDatabase("oncobridge")
        .WithUsername("oncobridge")
        .WithPassword("oncobridge")
        .Build();

    private OncoBridgeApiFactory? _factory;

    private OncoBridgeApiFactory? _boundedFactory;

    internal const int BoundedMaxPayloadBytes = 512;

    internal HttpClient Client { get; private set; } = null!;

    internal HttpClient BoundedClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _factory = new OncoBridgeApiFactory(_container.GetConnectionString());
        _boundedFactory = new OncoBridgeApiFactory(
            _container.GetConnectionString(),
            OncoBridgeApiFactory.Development,
            new BundleIngestionOptions { MaxPayloadBytes = BoundedMaxPayloadBytes });

        await MigrateAsync(_factory);

        Client = _factory.CreateClient();
        BoundedClient = _boundedFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        BoundedClient?.Dispose();

        if (_boundedFactory is not null)
        {
            await _boundedFactory.DisposeAsync();
        }

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    internal async Task<int> CountImportBatchesAsync()
    {
        using IServiceScope scope = _factory!.Services.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<OncoBridgeDbContext>()
            .ImportBatches.CountAsync();
    }

    private static async Task MigrateAsync(OncoBridgeApiFactory factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<OncoBridgeDbContext>()
            .Database.MigrateAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiPostgreSqlCollection : ICollectionFixture<ApiPostgreSqlFixture>
{
    public const string Name = "api-postgresql";
}
