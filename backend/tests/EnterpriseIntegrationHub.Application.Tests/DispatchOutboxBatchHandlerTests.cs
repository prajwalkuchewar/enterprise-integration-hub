using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;
using EnterpriseIntegrationHub.Application.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class DispatchOutboxBatchHandlerTests
{
    private const string Owner = "worker-a";
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IExecutionDispatchOutboxRepository> _repository = new();
    private readonly Mock<IExecutionDispatchPublisher> _publisher = new();
    private readonly DispatchOutboxOptions _options = new();
    private readonly DispatchOutboxBatchHandler _handler;

    public DispatchOutboxBatchHandlerTests()
    {
        _handler = new DispatchOutboxBatchHandler(_repository.Object, _publisher.Object);
    }

    private static ExecutionDispatchMessage CreateMessage(int attempt = 1) => new(
        Guid.NewGuid(), attempt, Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(),
        "evt-1", "order.created", Now, "corr-1");

    private void SetupClaim(params ExecutionDispatchMessage[] messages) =>
        _repository.Setup(r => r.ClaimBatchAsync(Owner, Now, TimeSpan.FromSeconds(_options.LeaseSeconds), _options.BatchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ExecutionDispatchMessage>)messages);

    [Fact]
    public async Task DispatchBatchAsync_WhenPublishSucceeds_MarksDispatched()
    {
        var message = CreateMessage();
        SetupClaim(message);
        _repository.Setup(r => r.MarkDispatchedAsync(message.DispatchId, Owner, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.DispatchBatchAsync(_options, Owner, Now, CancellationToken.None);

        result.Should().BeEquivalentTo(new DispatchOutboxBatchResult(Claimed: 1, Dispatched: 1, Failed: 0, LeaseLost: 0));
        _publisher.Verify(p => p.PublishAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.RecordFailureAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchBatchAsync_WhenPublishThrows_RecordsFailureWithRetryDelay()
    {
        var message = CreateMessage(attempt: 3);
        SetupClaim(message);
        _publisher.Setup(p => p.PublishAsync(message, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker unavailable"));
        _repository.Setup(r => r.RecordFailureAsync(message.DispatchId, Owner, It.IsAny<DateTimeOffset>(), "broker unavailable", It.IsAny<TimeSpan>(), _options.MaxAttempts, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.DispatchBatchAsync(_options, Owner, Now, CancellationToken.None);

        result.Should().BeEquivalentTo(new DispatchOutboxBatchResult(Claimed: 1, Dispatched: 0, Failed: 1, LeaseLost: 0));
        _repository.Verify(r => r.RecordFailureAsync(
            message.DispatchId,
            Owner,
            It.IsAny<DateTimeOffset>(),
            "broker unavailable",
            DispatchOutboxBatchHandler.ComputeRetryDelay(3, _options),
            _options.MaxAttempts,
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.MarkDispatchedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchBatchAsync_WhenMarkFencedOut_CountsAsLeaseLost()
    {
        var message = CreateMessage();
        SetupClaim(message);
        _repository.Setup(r => r.MarkDispatchedAsync(message.DispatchId, Owner, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.DispatchBatchAsync(_options, Owner, Now, CancellationToken.None);

        result.Should().BeEquivalentTo(new DispatchOutboxBatchResult(Claimed: 1, Dispatched: 0, Failed: 0, LeaseLost: 1));
    }

    [Fact]
    public async Task DispatchBatchAsync_WhenNothingClaimed_ReturnsZeroCounts()
    {
        SetupClaim();

        var result = await _handler.DispatchBatchAsync(_options, Owner, Now, CancellationToken.None);

        result.Should().BeEquivalentTo(new DispatchOutboxBatchResult(0, 0, 0, 0));
        _publisher.Verify(p => p.PublishAsync(It.IsAny<ExecutionDispatchMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(20, 300)]
    [InlineData(50, 300)]
    public void ComputeRetryDelay_IsExponentialAndCapped(int attempt, int expectedSeconds)
    {
        var delay = DispatchOutboxBatchHandler.ComputeRetryDelay(attempt, new DispatchOutboxOptions());

        delay.Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }
}
