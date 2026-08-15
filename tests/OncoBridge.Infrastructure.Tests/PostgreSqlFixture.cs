using Microsoft.EntityFrameworkCore;
using Npgsql;
using OncoBridge.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace OncoBridge.Infrastructure.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public const string PostgreSqlImage = "postgres:18.6";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(PostgreSqlImage)
        .WithDatabase("oncobridge")
        .WithUsername("oncobridge")
        .WithPassword("oncobridge")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<OncoBridgeDbContext> CreateMigratedContextAsync()
    {
        string databaseName = $"ob_{Guid.NewGuid():N}";

        await using (NpgsqlConnection admin = new(_container.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using NpgsqlCommand create = new($"""CREATE DATABASE "{databaseName}" """, admin);
            await create.ExecuteNonQueryAsync();
        }

        OncoBridgeDbContext context = new(
            new DbContextOptionsBuilder<OncoBridgeDbContext>()
                .UseNpgsql(ConnectionStringFor(databaseName))
                .Options);

        await context.Database.MigrateAsync();

        return context;
    }

    public string ConnectionStringFor(string databaseName) =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = databaseName }
            .ConnectionString;
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "postgresql";
}
