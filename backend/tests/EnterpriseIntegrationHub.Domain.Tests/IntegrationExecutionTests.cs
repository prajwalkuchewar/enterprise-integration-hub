using System;
using System.Linq;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Domain.Tests;

public sealed class IntegrationExecutionTests
{
    [Fact]
    public void Attempts_MustRunSequentially_AndCompleteExecutionOnlyAfterAllStepsSucceed()
    {
        var execution = CreateExecution();
        var now = DateTimeOffset.UtcNow;
        var beforeStart = () => execution.StartStepAttempt(1, now);
        beforeStart.Should().Throw<InvalidOperationException>();
        execution.Start(now);

        var startSecondEarly = () => execution.StartStepAttempt(2, now);
        startSecondEarly.Should().Throw<InvalidOperationException>();
        var firstAttempt = execution.StartStepAttempt(1, now);
        var duplicateAttempt = () => execution.StartStepAttempt(1, now);
        duplicateAttempt.Should().Throw<InvalidOperationException>();
        var finishSecondEarly = () => execution.MarkStepSucceeded(2);
        finishSecondEarly.Should().Throw<InvalidOperationException>();

        execution.MarkStepSucceeded(1);
        execution.Status.Should().Be(IntegrationExecutionStatus.Running);
        var secondAttempt = execution.StartStepAttempt(2, now);
        execution.MarkStepSucceeded(2);

        execution.Status.Should().Be(IntegrationExecutionStatus.Succeeded);
        execution.CompletedAt.Should().NotBeNull();
        firstAttempt.Status.Should().Be(ExecutionStepAttemptStatus.Succeeded);
        secondAttempt.Status.Should().Be(ExecutionStepAttemptStatus.Succeeded);
        execution.Steps.Should().OnlyContain(step => step.Status == ExecutionStepStatus.Succeeded);
    }

    [Fact]
    public void Retry_WaitsUntilDueAndDoesNotRerunSuccessfulSteps()
    {
        var execution = CreateExecution();
        var now = DateTimeOffset.UtcNow;
        var retryAt = now.AddMinutes(1);
        execution.Start(now);
        execution.StartStepAttempt(1, now);
        execution.MarkStepSucceeded(1);
        var failedAttempt = execution.StartStepAttempt(2, now);
        execution.MarkStepFailed(2, "temporary", "Destination unavailable.", retryAt);

        execution.Status.Should().Be(IntegrationExecutionStatus.RetryScheduled);
        var startEarly = () => execution.Start(retryAt.AddTicks(-1));
        startEarly.Should().Throw<InvalidOperationException>();
        execution.Status.Should().Be(IntegrationExecutionStatus.RetryScheduled);

        execution.Start(retryAt);
        var repeatSucceededStep = () => execution.StartStepAttempt(1, retryAt);
        repeatSucceededStep.Should().Throw<InvalidOperationException>();
        var retryAttempt = execution.StartStepAttempt(2, retryAt);
        retryAttempt.AttemptNumber.Should().Be(2);
        execution.MarkStepSucceeded(2);

        execution.Status.Should().Be(IntegrationExecutionStatus.Succeeded);
        failedAttempt.Status.Should().Be(ExecutionStepAttemptStatus.Failed);
        failedAttempt.FailureCode.Should().Be("temporary");
        retryAttempt.Status.Should().Be(ExecutionStepAttemptStatus.Succeeded);
        execution.Steps.Single(step => step.ExecutionOrder == 1).Attempts.Should().ContainSingle();
    }

    [Fact]
    public void PermanentFailure_SkipsLaterSteps()
    {
        var execution = CreateExecution();
        execution.Start();
        var attempt = execution.StartStepAttempt(1);

        execution.MarkStepFailed(1, "invalid", "Destination rejected the request.", null);

        execution.Status.Should().Be(IntegrationExecutionStatus.Failed);
        execution.CompletedAt.Should().NotBeNull();
        attempt.Status.Should().Be(ExecutionStepAttemptStatus.Failed);
        execution.Steps.Single(step => step.ExecutionOrder == 1).Status.Should().Be(ExecutionStepStatus.Failed);
        execution.Steps.Single(step => step.ExecutionOrder == 2).Status.Should().Be(ExecutionStepStatus.Skipped);
        var restart = () => execution.Start();
        restart.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancellation_IsRejectedDuringAnAttemptAndAllowedBetweenAttempts()
    {
        var execution = CreateExecution();
        execution.Start();
        execution.StartStepAttempt(1);
        var cancelDuringAttempt = () => execution.Cancel();
        cancelDuringAttempt.Should().Throw<InvalidOperationException>();

        execution.MarkStepFailed(1, "temporary", "Destination unavailable.", DateTimeOffset.UtcNow.AddMinutes(1));
        execution.Cancel();

        execution.Status.Should().Be(IntegrationExecutionStatus.Cancelled);
        execution.Steps.Should().OnlyContain(step => step.Status == ExecutionStepStatus.Skipped);
    }

    [Fact]
    public void InvalidFailureData_LeavesAttemptRunning()
    {
        var execution = CreateExecution();
        execution.Start();
        var attempt = execution.StartStepAttempt(1);
        var fail = () => execution.MarkStepFailed(1, "", "Failure details.", null);

        fail.Should().Throw<ArgumentException>();
        execution.Status.Should().Be(IntegrationExecutionStatus.Running);
        attempt.Status.Should().Be(ExecutionStepAttemptStatus.Running);
    }

    private static IntegrationExecution CreateExecution()
    {
        var sourceId = Guid.NewGuid();
        var firstDestination = Guid.NewGuid();
        var secondDestination = Guid.NewGuid();
        var workflow = new Workflow("Test workflow", sourceId, "order.created",
            [new(firstDestination, 1), new(secondDestination, 2)]);
        workflow.Activate();
        var revision = new WorkflowRevision(workflow);
        var steps = revision.Steps.Select(step => new ExecutionStepSnapshot(step.Id, step.DestinationConnectorId,
            "https://destination.example.test", ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, 30, step.ExecutionOrder));
        return new IntegrationExecution(revision, sourceId, "event-1", DateTimeOffset.UtcNow, "correlation-1",
            new string('a', 64), steps);
    }
}
