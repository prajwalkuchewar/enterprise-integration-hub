using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class ExecutionStepAttempt
{
    private ExecutionStepAttempt() { }

    internal ExecutionStepAttempt(Guid executionStepId, int attemptNumber, DateTimeOffset startedAt)
    {
        Id = Guid.NewGuid();
        ExecutionStepId = executionStepId;
        AttemptNumber = attemptNumber;
        Status = ExecutionStepAttemptStatus.Running;
        StartedAt = startedAt;
    }

    public Guid Id { get; private set; }
    public Guid ExecutionStepId { get; private set; }
    public int AttemptNumber { get; private set; }
    public ExecutionStepAttemptStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public int? HttpStatusCode { get; private set; }
    public long? DurationMilliseconds { get; private set; }

    internal void Succeed(DateTimeOffset completedAt, int? httpStatusCode, long? durationMilliseconds)
    {
        EnsureRunning();
        ValidateDiagnostics(httpStatusCode, durationMilliseconds);
        Status = ExecutionStepAttemptStatus.Succeeded;
        CompletedAt = completedAt;
        HttpStatusCode = httpStatusCode;
        DurationMilliseconds = durationMilliseconds;
    }

    internal void Fail(string failureCode, string failureMessage, DateTimeOffset completedAt, int? httpStatusCode, long? durationMilliseconds)
    {
        EnsureRunning();
        if (string.IsNullOrWhiteSpace(failureCode))
            throw new ArgumentException("FailureCode is required.", nameof(failureCode));
        if (string.IsNullOrWhiteSpace(failureMessage))
            throw new ArgumentException("FailureMessage is required.", nameof(failureMessage));
        ValidateDiagnostics(httpStatusCode, durationMilliseconds);

        Status = ExecutionStepAttemptStatus.Failed;
        CompletedAt = completedAt;
        FailureCode = failureCode.Length <= 100 ? failureCode : failureCode[..100];
        FailureMessage = failureMessage.Length <= 2000 ? failureMessage : failureMessage[..2000];
        HttpStatusCode = httpStatusCode;
        DurationMilliseconds = durationMilliseconds;
    }

    private static void ValidateDiagnostics(int? httpStatusCode, long? durationMilliseconds)
    {
        if (httpStatusCode is < 100 or > 599)
            throw new ArgumentOutOfRangeException(nameof(httpStatusCode));
        if (durationMilliseconds is < 0)
            throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
    }

    private void EnsureRunning()
    {
        if (Status != ExecutionStepAttemptStatus.Running)
            throw new InvalidOperationException("Only a running attempt can be completed.");
    }
}
