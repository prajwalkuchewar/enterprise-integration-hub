using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using EnterpriseIntegrationHub.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnterpriseIntegrationHub.Infrastructure.Tests;

// Trigger DDL is built from test-only constant table names, not user input.
#pragma warning disable EF1002
[Collection(SqlServerCollection.Name)]
public sealed class SqlServerEventIngestionPersistenceTests(SqlServerFixture fixture)
{
    private const string SourceEventId = "evt-persistence";

    [Theory]
    [InlineData("EventReceipts")]
    [InlineData("ExecutionDispatchOutbox")]
    public async Task CreateIfAbsent_WhenAnyRowInTheTransactionFails_CommitsNothing(string failingTable)
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);
        var triggerName = $"TR_Fail_{failingTable}";
        await database.Context.Database.ExecuteSqlRawAsync(
            $"CREATE TRIGGER {triggerName} ON {failingTable} AFTER INSERT AS THROW 50001, 'Injected failure', 1;");

        await using (var failing = database.CreateContext())
        {
            var (receipt, execution, outbox) = Accept(source, SourceEventId);
            var act = () => new IntegrationExecutionRepository(failing).CreateIfAbsentAsync(receipt, execution, outbox, default);

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        await using (var afterFailure = database.CreateContext())
        {
            (await CountAcceptedAsync(afterFailure, source.ConnectorId)).Should().Be(new AcceptedRows(0, 0, 0));
        }

        await database.Context.Database.ExecuteSqlRawAsync($"DROP TRIGGER {triggerName};");

        await using var retry = database.CreateContext();
        var (retryReceipt, retryExecution, retryOutbox) = Accept(source, SourceEventId);
        var accepted = await new IntegrationExecutionRepository(retry)
            .CreateIfAbsentAsync(retryReceipt, retryExecution, retryOutbox, default);

        accepted.Id.Should().Be(retryExecution.Id);
        (await CountAcceptedAsync(retry, source.ConnectorId)).Should().Be(new AcceptedRows(1, 1, 1));
    }

    [Fact]
    public async Task CreateIfAbsent_WhenSameSourceEventIsSubmittedAgain_ReturnsExistingWithoutDuplicateRows()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);

        await using var first = database.CreateContext();
        var (firstReceipt, firstExecution, firstOutbox) = Accept(source, SourceEventId);
        var firstResult = await new IntegrationExecutionRepository(first)
            .CreateIfAbsentAsync(firstReceipt, firstExecution, firstOutbox, default);

        await using var second = database.CreateContext();
        var (secondReceipt, secondExecution, secondOutbox) = Accept(source, SourceEventId);
        var secondResult = await new IntegrationExecutionRepository(second)
            .CreateIfAbsentAsync(secondReceipt, secondExecution, secondOutbox, default);

        secondResult.Id.Should().Be(firstResult.Id);
        (await CountAcceptedAsync(second, source.ConnectorId)).Should().Be(new AcceptedRows(1, 1, 1));
    }

    [Fact]
    public async Task CreateIfAbsent_WhenDuplicatesRaceAcrossContexts_AcceptsExactlyOneEvent()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);

        var contexts = Enumerable.Range(0, 8).Select(_ => database.CreateContext()).ToList();
        try
        {
            var attempts = contexts.Select(context =>
            {
                var (receipt, execution, outbox) = Accept(source, SourceEventId);
                return new IntegrationExecutionRepository(context).CreateIfAbsentAsync(receipt, execution, outbox, default);
            });

            var results = await Task.WhenAll(attempts);

            results.Select(result => result.Id).Distinct().Should().HaveCount(1);
            await using var verify = database.CreateContext();
            (await CountAcceptedAsync(verify, source.ConnectorId)).Should().Be(new AcceptedRows(1, 1, 1));
        }
        finally
        {
            foreach (var context in contexts)
                await context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Database_RejectsSecondReceiptForSameSourceEvent()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);

        await using (var first = database.CreateContext())
        {
            var (receipt, execution, outbox) = Accept(source, SourceEventId);
            await new IntegrationExecutionRepository(first).CreateIfAbsentAsync(receipt, execution, outbox, default);
        }

        await using var direct = database.CreateContext();
        var (duplicateReceipt, duplicateExecution, duplicateOutbox) = Accept(source, SourceEventId);
        direct.IntegrationExecutions.Add(duplicateExecution);
        direct.EventReceipts.Add(duplicateReceipt);
        direct.ExecutionDispatchOutbox.Add(duplicateOutbox);

        var act = () => direct.SaveChangesAsync();

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<SqlException>()
            .Which.Number.Should().BeOneOf(2601, 2627);
    }

    private sealed record EventSource(Guid ConnectorId, WorkflowRevision Revision, IReadOnlyList<ExecutionStepSnapshot> Steps);

    private sealed record AcceptedRows(int Executions, int Receipts, int Outbox);

    private static (EventReceipt Receipt, IntegrationExecution Execution, ExecutionDispatchOutbox Outbox) Accept(EventSource source, string eventId)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray()));
        var execution = new IntegrationExecution(source.Revision, source.ConnectorId, eventId, DateTimeOffset.UtcNow, "corr-1", fingerprint, source.Steps);
        return (new EventReceipt(execution), execution, new ExecutionDispatchOutbox(execution.Id));
    }

    private static async Task<AcceptedRows> CountAcceptedAsync(EnterpriseIntegrationHubDbContext context, Guid connectorId) => new(
        await context.IntegrationExecutions.CountAsync(execution => execution.SourceConnectorId == connectorId),
        await context.EventReceipts.CountAsync(receipt => receipt.SourceConnectorId == connectorId),
        await context.ExecutionDispatchOutbox.CountAsync(outbox => context.IntegrationExecutions
            .Any(execution => execution.Id == outbox.IntegrationExecutionId && execution.SourceConnectorId == connectorId)));

    private static async Task<EventSource> SeedSourceAsync(EnterpriseIntegrationHubDbContext context)
    {
        var externalSystem = new ExternalSystem($"Orders-{Guid.NewGuid():N}", "Integration test system", ExternalSystemEnvironment.Production, ExternalSystemStatus.Active);
        context.ExternalSystems.Add(externalSystem);
        await context.SaveChangesAsync();

        var source = new Connector("Source", "Source connector", externalSystem.Id, "https://example.test/source",
            ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);
        var destination = new Connector("Destination", "Destination connector", externalSystem.Id, "https://example.test/destination",
            ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, ConnectorStatus.Active);
        context.Connectors.AddRange(source, destination);
        await context.SaveChangesAsync();

        var workflow = new Workflow($"Workflow-{Guid.NewGuid():N}", source.Id, "order.created", [new(destination.Id, 1)]);
        workflow.Activate();
        context.Workflows.Add(workflow);
        await context.SaveChangesAsync();

        var revision = new WorkflowRevision(workflow);
        context.WorkflowRevisions.Add(revision);
        await context.SaveChangesAsync();

        var steps = revision.Steps.Select(step => new ExecutionStepSnapshot(
            step.Id,
            destination.Id,
            destination.BaseUrl,
            destination.Protocol,
            destination.AuthenticationType,
            destination.TimeoutSeconds,
            step.ExecutionOrder)).ToList();

        context.ChangeTracker.Clear();
        return new EventSource(source.Id, revision, steps);
    }
}
