using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Executions.Worker;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public sealed class ExecutionWorkerHandlerTests
{
    private readonly Mock<IExecutionLeaseRepository> _leases = new();
    private readonly Mock<IExecutionAttemptRepository> _attempts = new();
    private readonly Mock<IExecutionDestinationInvoker> _destination = new();
    private readonly ExecutionLease _lease = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1));

    [Fact]
    public async Task RunOnce_WhenNoWorkExists_IsIdle()
    {
        _leases.Setup(item => item.ClaimAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecutionLease?)null);
        var result = await Handler().RunOnceAsync(new ExecutionWorkerOptions(), default);
        result.Should().Be(ExecutionWorkerRunStatus.Idle);
        _destination.Verify(item => item.InvokeAsync(It.IsAny<ExecutionAttemptWork>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnce_WhenLeaseIsLostBeforeInvocation_DoesNotCallDestination()
    {
        SetupClaim();
        _leases.SetupSequence(item => item.RenewAsync(_lease.DispatchId, _lease.Token, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);
        _attempts.Setup(item => item.BeginAsync(_lease, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.Ready, Work()));

        var result = await Handler().RunOnceAsync(new ExecutionWorkerOptions(), default);

        result.Should().Be(ExecutionWorkerRunStatus.LeaseLost);
        _destination.Verify(item => item.InvokeAsync(It.IsAny<ExecutionAttemptWork>(), It.IsAny<CancellationToken>()), Times.Never);
        _attempts.Verify(item => item.FinishAsync(It.IsAny<ExecutionLease>(), It.IsAny<Guid>(),
            It.IsAny<ExecutionAttemptOutcome>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnce_WhenDestinationTimesOut_SchedulesTransientRetry()
    {
        SetupClaim();
        _leases.Setup(item => item.RenewAsync(_lease.DispatchId, _lease.Token, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var work = Work(timeoutSeconds: 1);
        _attempts.Setup(item => item.BeginAsync(_lease, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.Ready, work));
        _destination.Setup(item => item.InvokeAsync(work, It.IsAny<CancellationToken>()))
            .Returns(async (ExecutionAttemptWork _, CancellationToken token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return DestinationInvocationResult.Succeeded;
            });
        ExecutionAttemptOutcome? saved = null;
        _attempts.Setup(item => item.FinishAsync(_lease, work.AttemptId, It.IsAny<ExecutionAttemptOutcome>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<ExecutionLease, Guid, ExecutionAttemptOutcome, int, CancellationToken>((_, _, outcome, _, _) => saved = outcome)
            .ReturnsAsync(FinishExecutionAttemptStatus.RetryScheduled);

        var result = await Handler().RunOnceAsync(new ExecutionWorkerOptions(), default);

        result.Should().Be(ExecutionWorkerRunStatus.RetryScheduled);
        saved.Should().NotBeNull();
        saved!.Failure.Should().Be(ExecutionAttemptFailure.TransientDestination);
        saved.RetryDelay.Should().NotBeNull();
        saved.RetryDelay!.Value.TotalSeconds.Should().BeInRange(0, 2);
    }

    [Fact]
    public async Task RunOnce_WhenCancellationIsRequested_DoesNotFinishAttempt()
    {
        SetupClaim();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var act = () => Handler().RunOnceAsync(new ExecutionWorkerOptions(), cancellation.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        _attempts.Verify(item => item.FinishAsync(It.IsAny<ExecutionLease>(), It.IsAny<Guid>(),
            It.IsAny<ExecutionAttemptOutcome>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunOnce_WhenLastAttemptFails_DoesNotScheduleAnotherRetry()
    {
        SetupClaim();
        _leases.Setup(item => item.RenewAsync(_lease.DispatchId, _lease.Token, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var work = Work() with { AttemptNumber = 2 };
        _attempts.Setup(item => item.BeginAsync(_lease, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BeginExecutionAttemptResult(BeginExecutionAttemptStatus.Ready, work));
        _destination.Setup(item => item.InvokeAsync(work, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DestinationInvocationResult.TransientFailure);
        ExecutionAttemptOutcome? saved = null;
        _attempts.Setup(item => item.FinishAsync(_lease, work.AttemptId, It.IsAny<ExecutionAttemptOutcome>(), 2, It.IsAny<CancellationToken>()))
            .Callback<ExecutionLease, Guid, ExecutionAttemptOutcome, int, CancellationToken>((_, _, outcome, _, _) => saved = outcome)
            .ReturnsAsync(FinishExecutionAttemptStatus.Completed);

        var result = await Handler().RunOnceAsync(new ExecutionWorkerOptions { MaxAttempts = 2 }, default);

        result.Should().Be(ExecutionWorkerRunStatus.Finished);
        saved.Should().NotBeNull();
        saved!.Failure.Should().Be(ExecutionAttemptFailure.TransientDestination);
        saved.RetryDelay.Should().BeNull();
    }

    [Fact]
    public async Task RunOnce_InvalidOptionsFailBeforeClaimingWork()
    {
        var act = () => Handler().RunOnceAsync(new ExecutionWorkerOptions { LeaseSeconds = 0 }, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _leases.Verify(item => item.ClaimAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void RetryDelay_IsJitteredAndNeverExceedsCap()
    {
        var options = new ExecutionWorkerOptions { BaseRetryDelaySeconds = 2, MaxRetryDelaySeconds = 10 };
        for (var index = 0; index < 100; index++)
        {
            ExecutionWorkerHandler.CalculateRetryDelay(1, options).TotalSeconds.Should().BeInRange(0, 2);
            ExecutionWorkerHandler.CalculateRetryDelay(20, options).TotalSeconds.Should().BeInRange(0, 10);
        }
    }

    private void SetupClaim() =>
        _leases.Setup(item => item.ClaimAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(_lease);

    private ExecutionWorkerHandler Handler() => new(_leases.Object, _attempts.Object, _destination.Object);

    private ExecutionAttemptWork Work(int timeoutSeconds = 30) =>
        new(_lease.ExecutionId, Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid(), "https://example.test",
            ConnectorProtocol.REST, ConnectorAuthenticationType.APIKey, timeoutSeconds, "correlation-1", "{\"value\":1}");
}
