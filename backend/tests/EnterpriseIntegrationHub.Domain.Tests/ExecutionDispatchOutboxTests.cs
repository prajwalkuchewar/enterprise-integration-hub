using System;
using EnterpriseIntegrationHub.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Domain.Tests;

public class ExecutionDispatchOutboxTests
{
    // The constructor stamps NextAttemptAt from the real clock, so the test clock must start after it.
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.AddHours(1);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);

    [Fact]
    public void Lease_WhenDispatchable_SetsOwnerExpiryAndIncrementsAttempts()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());

        outbox.Lease("worker-a", Now, LeaseDuration);

        outbox.LeaseOwner.Should().Be("worker-a");
        outbox.LeaseExpiresAt.Should().Be(Now + LeaseDuration);
        outbox.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void Lease_WhenLeaseIsStillActive_Throws()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);

        var act = () => outbox.Lease("worker-b", Now.AddSeconds(10), LeaseDuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Lease_AfterExpiry_CanBeReclaimedByAnotherWorker()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);

        outbox.Lease("worker-b", Now + LeaseDuration, LeaseDuration);

        outbox.LeaseOwner.Should().Be("worker-b");
        outbox.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void IsDispatchable_WhenNextAttemptIsInTheFuture_IsFalse()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);
        outbox.RecordFailure("worker-a", Now, "broker down", TimeSpan.FromSeconds(30), maxAttempts: 8);

        outbox.IsDispatchable(Now.AddSeconds(29)).Should().BeFalse();
        outbox.IsDispatchable(Now.AddSeconds(30)).Should().BeTrue();
    }

    [Fact]
    public void MarkDispatched_WithHeldLease_SetsDispatchedAndReleasesLease()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);

        outbox.MarkDispatched("worker-a", Now);

        outbox.DispatchedAt.Should().Be(Now);
        outbox.LeaseOwner.Should().BeNull();
        outbox.LeaseExpiresAt.Should().BeNull();
        outbox.IsDispatchable(Now.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void MarkDispatched_WithDifferentOwner_Throws()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);

        var act = () => outbox.MarkDispatched("worker-b", Now);

        act.Should().Throw<InvalidOperationException>();
        outbox.DispatchedAt.Should().BeNull();
    }

    [Fact]
    public void RecordFailure_BelowMaxAttempts_SchedulesRetryAndReleasesLease()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);

        outbox.RecordFailure("worker-a", Now, "broker down", TimeSpan.FromSeconds(4), maxAttempts: 8);

        outbox.NextAttemptAt.Should().Be(Now.AddSeconds(4));
        outbox.LastError.Should().Be("broker down");
        outbox.LeaseOwner.Should().BeNull();
        outbox.DeadLetteredAt.Should().BeNull();
    }

    [Fact]
    public void RecordFailure_AtMaxAttempts_DeadLetters()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);

        outbox.RecordFailure("worker-a", Now, "permanent", TimeSpan.FromSeconds(4), maxAttempts: 1);

        outbox.DeadLetteredAt.Should().Be(Now);
        outbox.IsDispatchable(Now.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void RecordFailure_WithLongError_TruncatesTo2000Characters()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);

        outbox.RecordFailure("worker-a", Now, new string('x', 5000), TimeSpan.FromSeconds(1), maxAttempts: 8);

        outbox.LastError.Should().HaveLength(2000);
    }

    [Fact]
    public void RecordFailure_WithDifferentOwner_Throws()
    {
        var outbox = new ExecutionDispatchOutbox(Guid.NewGuid());
        outbox.Lease("worker-a", Now, LeaseDuration);

        var act = () => outbox.RecordFailure("worker-b", Now, "error", TimeSpan.FromSeconds(1), maxAttempts: 8);

        act.Should().Throw<InvalidOperationException>();
    }
}
