using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using EnterpriseIntegrationHub.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Infrastructure.Tests;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerExecutionDispatchOutboxTests(SqlServerFixture fixture)
{
    // Outbox rows are stamped with the real clock on insert, so the test clock must start after them.
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.AddHours(1);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task ClaimBatch_ReturnsDispatchableRowsWithExecutionDetails()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        var outboxId = await SeedOutboxAsync(database.Context);

        await using var repositoryContext = database.CreateContext();
        var repository = new ExecutionDispatchOutboxRepository(repositoryContext);

        var claimed = await repository.ClaimBatchAsync("worker-a", Now, LeaseDuration, 10, default);

        claimed.Should().ContainSingle();
        var message = claimed[0];
        message.DispatchId.Should().Be(outboxId);
        message.Attempt.Should().Be(1);
        message.TriggerEvent.Should().Be("order.created");
        message.SourceEventId.Should().Be("evt-1");
    }

    [Fact]
    public async Task ClaimBatch_WhenLeaseIsActive_OtherWorkerReceivesNothing()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        await SeedOutboxAsync(database.Context);

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();

        (await new ExecutionDispatchOutboxRepository(first).ClaimBatchAsync("worker-a", Now, LeaseDuration, 10, default))
            .Should().ContainSingle();
        (await new ExecutionDispatchOutboxRepository(second).ClaimBatchAsync("worker-b", Now, LeaseDuration, 10, default))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task ClaimBatch_AfterLeaseExpires_ReclaimsAndIncrementsAttempt()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        await SeedOutboxAsync(database.Context);

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        await new ExecutionDispatchOutboxRepository(first).ClaimBatchAsync("worker-a", Now, LeaseDuration, 10, default);

        var reclaimed = await new ExecutionDispatchOutboxRepository(second)
            .ClaimBatchAsync("worker-b", Now + LeaseDuration, LeaseDuration, 10, default);

        reclaimed.Should().ContainSingle();
        reclaimed[0].Attempt.Should().Be(2);
    }

    [Fact]
    public async Task MarkDispatched_AfterLeaseWasReclaimed_IsRejected()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        await SeedOutboxAsync(database.Context);

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var staleWorker = new ExecutionDispatchOutboxRepository(first);
        var activeWorker = new ExecutionDispatchOutboxRepository(second);

        var staleClaim = await staleWorker.ClaimBatchAsync("worker-a", Now, LeaseDuration, 10, default);
        var activeClaim = await activeWorker.ClaimBatchAsync("worker-b", Now + LeaseDuration, LeaseDuration, 10, default);

        var staleMark = await staleWorker.MarkDispatchedAsync(staleClaim[0].DispatchId, "worker-a", Now + LeaseDuration, default);
        staleMark.Should().BeFalse();

        var activeMark = await activeWorker.MarkDispatchedAsync(activeClaim[0].DispatchId, "worker-b", Now + LeaseDuration, default);
        activeMark.Should().BeTrue();
    }

    [Fact]
    public async Task MarkDispatched_RemovesRowFromFutureClaims()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        await SeedOutboxAsync(database.Context);

        await using var worker = database.CreateContext();
        var claimed = await new ExecutionDispatchOutboxRepository(worker).ClaimBatchAsync("worker-a", Now, LeaseDuration, 10, default);
        await new ExecutionDispatchOutboxRepository(worker).MarkDispatchedAsync(claimed[0].DispatchId, "worker-a", Now, default);

        await using var later = database.CreateContext();
        var remaining = await new ExecutionDispatchOutboxRepository(later).ClaimBatchAsync("worker-a", Now.AddDays(1), LeaseDuration, 10, default);

        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordFailure_AtMaxAttempts_DeadLettersAndStopsClaims()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        await SeedOutboxAsync(database.Context);

        await using var worker = database.CreateContext();
        var claimed = await new ExecutionDispatchOutboxRepository(worker).ClaimBatchAsync("worker-a", Now, LeaseDuration, 10, default);

        var recorded = await new ExecutionDispatchOutboxRepository(worker).RecordFailureAsync(
            claimed[0].DispatchId, "worker-a", Now, "permanent", TimeSpan.FromSeconds(2), maxAttempts: 1, default);

        recorded.Should().BeTrue();

        await using var later = database.CreateContext();
        (await new ExecutionDispatchOutboxRepository(later).ClaimBatchAsync("worker-a", Now.AddDays(1), LeaseDuration, 10, default)).Should().BeEmpty();
    }

    [Fact]
    public async Task RecordFailure_BelowMaxAttempts_SchedulesRetryAfterDelay()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        await SeedOutboxAsync(database.Context);

        await using var worker = database.CreateContext();
        var claimed = await new ExecutionDispatchOutboxRepository(worker).ClaimBatchAsync("worker-a", Now, LeaseDuration, 10, default);
        await new ExecutionDispatchOutboxRepository(worker).RecordFailureAsync(
            claimed[0].DispatchId, "worker-a", Now, "broker down", TimeSpan.FromSeconds(30), maxAttempts: 8, default);

        await using var beforeRetry = database.CreateContext();
        (await new ExecutionDispatchOutboxRepository(beforeRetry).ClaimBatchAsync("worker-a", Now.AddSeconds(29), LeaseDuration, 10, default))
            .Should().BeEmpty();

        await using var atRetry = database.CreateContext();
        (await new ExecutionDispatchOutboxRepository(atRetry).ClaimBatchAsync("worker-a", Now.AddSeconds(30), LeaseDuration, 10, default))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task ConcurrentClaims_NeverReturnSameRowTwice()
    {
        await using var database = await SqlServerRelationalIntegrationTests.TestDatabase.CreateAsync(fixture);
        for (var index = 0; index < 10; index++)
            await SeedOutboxAsync(database.Context);

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var firstClaims = new ExecutionDispatchOutboxRepository(first);
        var secondClaims = new ExecutionDispatchOutboxRepository(second);

        var results = await Task.WhenAll(
            firstClaims.ClaimBatchAsync("worker-a", Now, LeaseDuration, 10, default),
            secondClaims.ClaimBatchAsync("worker-b", Now, LeaseDuration, 10, default));

        var claimedIds = results.SelectMany(batch => batch.Select(message => message.DispatchId)).ToList();
        claimedIds.Should().OnlyHaveUniqueItems();
        claimedIds.Count.Should().BeLessOrEqualTo(10);
    }

    private static async Task<Guid> SeedOutboxAsync(EnterpriseIntegrationHubDbContext context)
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

        var fingerprint = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray()));
        var snapshots = revision.Steps.Select(step => new ExecutionStepSnapshot(
            step.Id,
            destination.Id,
            destination.BaseUrl,
            destination.Protocol,
            destination.AuthenticationType,
            destination.TimeoutSeconds,
            step.ExecutionOrder)).ToList();
        var execution = new IntegrationExecution(revision, source.Id, "evt-1", Now, "corr-1", fingerprint, snapshots);
        context.IntegrationExecutions.Add(execution);
        await context.SaveChangesAsync();

        var outbox = new ExecutionDispatchOutbox(execution.Id);
        context.ExecutionDispatchOutbox.Add(outbox);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return outbox.Id;
    }
}
