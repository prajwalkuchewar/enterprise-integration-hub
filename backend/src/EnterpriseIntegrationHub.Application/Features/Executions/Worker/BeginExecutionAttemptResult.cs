namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public enum BeginExecutionAttemptStatus
{
    LeaseLost,
    Completed,
    Ready
}

public sealed record BeginExecutionAttemptResult(BeginExecutionAttemptStatus Status, ExecutionAttemptWork? Work);
