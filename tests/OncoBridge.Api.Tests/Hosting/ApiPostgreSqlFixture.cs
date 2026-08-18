using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OncoBridge.Infrastructure.Persistence;
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

    internal HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _factory = new OncoBridgeApiFactory(_container.GetConnectionString());

        await MigrateAsync(_factory);

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync();
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
