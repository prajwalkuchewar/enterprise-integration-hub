using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class ExecutionStepAttempt
{
    private ExecutionStepAttempt() { }

    internal ExecutionStepAttempt(Guid executionStepId, int attemptNumber)
    {
        Id = Guid.NewGuid();
        ExecutionStepId = executionStepId;
        AttemptNumber = attemptNumber;
        Status = ExecutionStepAttemptStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ExecutionStepId { get; private set; }
    public int AttemptNumber { get; private set; }
    public ExecutionStepAttemptStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }

    internal void Succeed()
    {
        EnsureRunning();
        Status = ExecutionStepAttemptStatus.Succeeded;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    internal void Fail(string failureCode, string failureMessage)
    {
        EnsureRunning();
        if (string.IsNullOrWhiteSpace(failureCode))
            throw new ArgumentException("FailureCode is required.", nameof(failureCode));
        if (string.IsNullOrWhiteSpace(failureMessage))
            throw new ArgumentException("FailureMessage is required.", nameof(failureMessage));

        Status = ExecutionStepAttemptStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        FailureCode = failureCode.Length <= 100 ? failureCode : failureCode[..100];
        FailureMessage = failureMessage.Length <= 2000 ? failureMessage : failureMessage[..2000];
    }

    private void EnsureRunning()
    {
        if (Status != ExecutionStepAttemptStatus.Running)
            throw new InvalidOperationException("Only a running attempt can be completed.");
    }
}
