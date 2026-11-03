using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using EnterpriseIntegrationHub.Infrastructure.Messaging.Kafka;
using EnterpriseIntegrationHub.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
    public async Task Inbox_WhenDispatchIsDeliveredConcurrently_StoresExactlyOneItem()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);
        var (receipt, execution, outbox) = Accept(source, SourceEventId);
        await using (var context = database.CreateContext())
            await new IntegrationExecutionRepository(context).CreateIfAbsentAsync(receipt, execution, outbox, default);
        var message = Dispatch(execution, outbox);

        var contexts = Enumerable.Range(0, 8).Select(_ => database.CreateContext()).ToList();
        try
        {
            await Task.WhenAll(contexts.Select(context => new ExecutionDispatchInboxWriter(context).StoreAsync(message, default)));
            await using var verify = database.CreateContext();
            var stored = await verify.ExecutionDispatchInbox.SingleAsync();
            stored.DispatchId.Should().Be(outbox.Id);
            stored.IntegrationExecutionId.Should().Be(execution.Id);
        }
        finally
        {
            foreach (var context in contexts)
                await context.DisposeAsync();
        }
    }

    [KafkaIntegrationFact]
    public async Task Kafka_RedeliveredDispatch_IsDurablyStoredBeforeOffsetsAreCommitted()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);
        var (receipt, execution, outbox) = Accept(source, SourceEventId);
        await using (var context = database.CreateContext())
            await new IntegrationExecutionRepository(context).CreateIfAbsentAsync(receipt, execution, outbox, default);

        var bootstrap = Environment.GetEnvironmentVariable("EIH_KAFKA_TEST_BOOTSTRAP")!;
        var topic = $"eih-test-dispatch-{Guid.NewGuid():N}";
        var group = $"eih-test-consumer-{Guid.NewGuid():N}";
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrap }).Build();
        await admin.CreateTopicsAsync([new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }]);
        try
        {
            var message = Dispatch(execution, outbox);
            using var publisher = new KafkaExecutionDispatchPublisher(new KafkaOptions
            {
                BootstrapServers = bootstrap,
                ExecutionDispatchTopic = topic
            });
            await publisher.PublishAsync(message, default);
            await publisher.PublishAsync(message, default);

            using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = bootstrap,
                GroupId = group,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).Build();
            consumer.Subscribe(topic);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            for (var index = 0; index < 2; index++)
            {
                var record = consumer.Consume(timeout.Token);
                await using var context = database.CreateContext();
                await new ExecutionDispatchRecordProcessor(new ExecutionDispatchInboxWriter(context))
                    .ProcessAsync(record, consumer, timeout.Token);
            }

            await using var verify = database.CreateContext();
            (await verify.ExecutionDispatchInbox.CountAsync()).Should().Be(1);
            var committed = consumer.Committed([new TopicPartition(topic, 0)], TimeSpan.FromSeconds(10));
            committed.Single().Offset.Value.Should().Be(2);
            consumer.Close();
        }
        finally
        {
            await admin.DeleteTopicsAsync([topic]);
        }
    }

    [Fact]
    public async Task Inbox_WhenDispatchDoesNotMatchExecution_RejectsWithoutStoring()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);
        var (receipt, execution, outbox) = Accept(source, SourceEventId);
        await using (var context = database.CreateContext())
            await new IntegrationExecutionRepository(context).CreateIfAbsentAsync(receipt, execution, outbox, default);

        await using var consumer = database.CreateContext();
        await new ExecutionDispatchInboxWriter(consumer).StoreAsync(Dispatch(execution, outbox), default);
        var message = Dispatch(execution, outbox) with { CorrelationId = "unexpected" };
        var act = () => new ExecutionDispatchInboxWriter(consumer).StoreAsync(message, default);
        await act.Should().ThrowAsync<System.IO.InvalidDataException>();
        (await consumer.ExecutionDispatchInbox.CountAsync()).Should().Be(1);
    }

    private static ExecutionDispatchMessage Dispatch(IntegrationExecution execution, ExecutionDispatchOutbox outbox) =>
        new(outbox.Id, 1, execution.Id, execution.WorkflowId, execution.WorkflowRevisionNumber,
            execution.SourceConnectorId, execution.SourceEventId, execution.TriggerEvent,
            execution.OccurredAt, execution.CorrelationId);

    [Fact]
    public async Task CreateIfAbsent_PersistsPayloadAndSchemaVersionWithExecution()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);
        var (_, execution, outbox) = Accept(source, SourceEventId);
        var largeValue = new string('x', 5000);
        using var submittedPayload = JsonDocument.Parse(JsonSerializer.Serialize(new { nested = new { amount = 42, value = largeValue } }));
        var receipt = new EventReceipt(execution, 2, submittedPayload.RootElement);

        await using (var context = database.CreateContext())
            await new IntegrationExecutionRepository(context).CreateIfAbsentAsync(receipt, execution, outbox, default);

        await using var verify = database.CreateContext();
        var persisted = await verify.EventReceipts.SingleAsync(item => item.IntegrationExecutionId == execution.Id);
        persisted.SchemaVersion.Should().Be(2);
        using var payload = JsonDocument.Parse(persisted.PayloadJson!);
        payload.RootElement.GetProperty("nested").GetProperty("amount").GetInt32().Should().Be(42);
        payload.RootElement.GetProperty("nested").GetProperty("value").GetString().Should().Be(largeValue);
        (await CountAcceptedAsync(verify, source.ConnectorId)).Should().Be(new AcceptedRows(1, 1, 1));
    }

    [Fact]
    public async Task PayloadMigration_LeavesLegacyReceiptsWithoutFabricatedPayload()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var source = await SeedSourceAsync(database.Context);
        var (receipt, execution, outbox) = Accept(source, SourceEventId);
        await using (var context = database.CreateContext())
            await new IntegrationExecutionRepository(context).CreateIfAbsentAsync(receipt, execution, outbox, default);

        await using (var migrationContext = database.CreateContext())
        {
            var migrator = migrationContext.GetService<IMigrator>();
            await migrator.MigrateAsync("20261103135643_AddEventReceipts");
            await migrator.MigrateAsync();
        }

        await using var verify = database.CreateContext();
        var legacyReceipt = await verify.EventReceipts.SingleAsync(item => item.IntegrationExecutionId == execution.Id);
        legacyReceipt.PayloadJson.Should().BeNull();
        legacyReceipt.SchemaVersion.Should().BeNull();
        (await CountAcceptedAsync(verify, source.ConnectorId)).Should().Be(new AcceptedRows(1, 1, 1));
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
        using var payload = JsonDocument.Parse("{\"nested\":{\"amount\":42}}");
        return (new EventReceipt(execution, 2, payload.RootElement), execution, new ExecutionDispatchOutbox(execution.Id));
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
