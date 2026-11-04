namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public enum FinishExecutionAttemptStatus
{
    LeaseLost,
    ReadyForNextStep,
    RetryScheduled,
    Completed
}
