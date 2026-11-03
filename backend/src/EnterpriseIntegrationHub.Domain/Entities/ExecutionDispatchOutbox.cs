namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class ExecutionDispatchOutbox
{
    private const int MaxErrorLength = 2000;

    private ExecutionDispatchOutbox() { }

    public ExecutionDispatchOutbox(Guid integrationExecutionId)
    {
        if (integrationExecutionId == Guid.Empty)
            throw new ArgumentException("IntegrationExecutionId must be provided.", nameof(integrationExecutionId));

        Id = Guid.NewGuid();
        IntegrationExecutionId = integrationExecutionId;
        CreatedAt = DateTimeOffset.UtcNow;
        NextAttemptAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationExecutionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTimeOffset? LeaseExpiresAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? DeadLetteredAt { get; private set; }

    public bool IsDispatchable(DateTimeOffset now) =>
        DispatchedAt is null
        && DeadLetteredAt is null
        && NextAttemptAt <= now
        && (LeaseExpiresAt is null || LeaseExpiresAt <= now);

    public void Lease(string owner, DateTimeOffset now, TimeSpan leaseDuration)
    {
        ValidateOwner(owner);
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration), "The lease duration must be positive.");
        if (!IsDispatchable(now))
            throw new InvalidOperationException("The dispatch is not available for leasing.");

        LeaseOwner = owner;
        LeaseExpiresAt = now + leaseDuration;
        AttemptCount++;
    }

    public void MarkDispatched(string owner, DateTimeOffset now)
    {
        EnsureLeaseHeld(owner);
        DispatchedAt = now;
        LastError = null;
        ReleaseLease();
    }

    public void RecordFailure(string owner, DateTimeOffset now, string error, TimeSpan retryDelay, int maxAttempts)
    {
        EnsureLeaseHeld(owner);
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("An error description must be provided.", nameof(error));
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "At least one attempt is required.");

        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        ReleaseLease();

        if (AttemptCount >= maxAttempts)
        {
            DeadLetteredAt = now;
            return;
        }

        NextAttemptAt = now + retryDelay;
    }

    private void EnsureLeaseHeld(string owner)
    {
        ValidateOwner(owner);
        if (LeaseOwner != owner)
            throw new InvalidOperationException("The dispatch lease is held by another worker.");
    }

    private void ReleaseLease()
    {
        LeaseOwner = null;
        LeaseExpiresAt = null;
    }

    private static void ValidateOwner(string owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("A lease owner must be provided.", nameof(owner));
    }
}
