namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public enum ExecutionAttemptFailure
{
    TransientDestination,
    PermanentDestination,
    InvalidConfiguration
}

public sealed record ExecutionAttemptOutcome(
    bool Succeeded,
    ExecutionAttemptFailure? Failure,
    DateTimeOffset? RetryAt);
