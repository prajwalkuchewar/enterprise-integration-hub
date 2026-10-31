using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace EnterpriseIntegrationHub.Infrastructure.Tests;

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server integration tests";
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly string? _configuredConnectionString = Environment.GetEnvironmentVariable("EIH_SQLSERVER_TEST_CONNECTION");
    private readonly MsSqlContainer? _container;

    public SqlServerFixture()
    {
        if (string.IsNullOrWhiteSpace(_configuredConnectionString))
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Eih_Test_Password!2026")
                .Build();
        }
    }

    public Task InitializeAsync() =>
        _container is null ? Task.CompletedTask : _container.StartAsync();

    public Task DisposeAsync() =>
        _container is null ? Task.CompletedTask : _container.DisposeAsync().AsTask();

    public string GetConnectionString(string databaseName)
    {
        var baseConnectionString = _configuredConnectionString ?? _container!.GetConnectionString();
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }
}

