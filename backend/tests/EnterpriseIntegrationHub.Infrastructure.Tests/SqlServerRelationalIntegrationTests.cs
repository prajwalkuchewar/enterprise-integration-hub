using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnterpriseIntegrationHub.Infrastructure.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerRelationalIntegrationTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task Migrations_CreateExpectedTablesAndUniqueIndexes()
    {
        await using var database = await TestDatabase.CreateAsync(fixture);

        var tables = await database.Context.Database
            .SqlQueryRaw<string>("SELECT [name] AS [Value] FROM sys.tables WHERE [name] IN ('ExternalSystems', 'Connectors', 'Workflows', 'WorkflowSteps')")
            .ToListAsync();

        tables.Should().BeEquivalentTo(["ExternalSystems", "Connectors", "Workflows", "WorkflowSteps"]);

        var indexes = await database.Context.Database
            .SqlQueryRaw<string>("SELECT [name] AS [Value] FROM sys.indexes WHERE [name] IN ('IX_ExternalSystems_Name_Environment', 'IX_Connectors_Name_ExternalSystemId', 'IX_Workflows_Name')")
            .ToListAsync();

        indexes.Should().BeEquivalentTo([
            "IX_ExternalSystems_Name_Environment",
            "IX_Connectors_Name_ExternalSystemId",
            "IX_Workflows_Name"]);
    }

    [Fact]
    public async Task UniqueExternalSystemNameAndEnvironment_IsEnforcedBySqlServer()
    {
        await using var database = await TestDatabase.CreateAsync(fixture);
        var system1 = CreateExternalSystem("Orders", ExternalSystemEnvironment.Production);
        var system2 = CreateExternalSystem("orders", ExternalSystemEnvironment.Production);

        database.Context.ExternalSystems.AddRange(system1, system2);

        var action = () => database.Context.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException != null &&
                (((SqlException)exception.InnerException).Number == 2601 || ((SqlException)exception.InnerException).Number == 2627));
    }

    [Fact]
    public async Task ConnectorAndWorkflowForeignKeys_AreEnforcedBySqlServer()
    {
        await using var database = await TestDatabase.CreateAsync(fixture);
        var invalidConnector = CreateConnector(Guid.NewGuid(), "Orphan connector");
        database.Context.Connectors.Add(invalidConnector);

        var connectorSave = () => database.Context.SaveChangesAsync();
        await connectorSave.Should().ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException != null && ((SqlException)exception.InnerException).Number == 547);

        database.Context.ChangeTracker.Clear();
        var system = CreateExternalSystem("Orders", ExternalSystemEnvironment.Production);
        database.Context.ExternalSystems.Add(system);
        var connector = CreateConnector(system.Id, "Orders connector");
        database.Context.Connectors.Add(connector);
        await database.Context.SaveChangesAsync();

        var invalidWorkflow = new Workflow("Orphan workflow", Guid.NewGuid(), "OrderCreated", [new(Guid.NewGuid(), 1)]);
        database.Context.Workflows.Add(invalidWorkflow);

        var workflowSave = () => database.Context.SaveChangesAsync();
        await workflowSave.Should().ThrowAsync<DbUpdateException>()
            .Where(exception => exception.InnerException != null && ((SqlException)exception.InnerException).Number == 547);
    }

    private static ExternalSystem CreateExternalSystem(string name, ExternalSystemEnvironment environment) =>
        new(name, "Integration test system", environment, ExternalSystemStatus.Active);

    private static Connector CreateConnector(Guid externalSystemId, string name) =>
        new(name, "Integration test connector", externalSystemId, "https://example.test",
            ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);

    private sealed class TestDatabase(SqlServerFixture fixture, string databaseName, EnterpriseIntegrationHubDbContext context) : IAsyncDisposable
    {
        public EnterpriseIntegrationHubDbContext Context { get; } = context;

        public static async Task<TestDatabase> CreateAsync(SqlServerFixture fixture)
        {
            var databaseName = $"EihTests_{Guid.NewGuid():N}";
            var options = new DbContextOptionsBuilder<EnterpriseIntegrationHubDbContext>()
                .UseSqlServer(fixture.GetConnectionString(databaseName))
                .Options;
            var context = new EnterpriseIntegrationHubDbContext(options);
            await context.Database.MigrateAsync();
            return new TestDatabase(fixture, databaseName, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            var masterOptions = new DbContextOptionsBuilder<EnterpriseIntegrationHubDbContext>()
                .UseSqlServer(fixture.GetConnectionString("master"))
                .Options;
            await using var master = new EnterpriseIntegrationHubDbContext(masterOptions);
            await using var connection = new SqlConnection(fixture.GetConnectionString("master"));
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}];";
            await command.ExecuteNonQueryAsync();
        }
    }
}

