namespace EnterpriseIntegrationHub.Domain.Enums;

public enum ExecutionStepStatus
{
    Pending = 1,
    Running = 2,
    RetryScheduled = 3,
    Succeeded = 4,
    Failed = 5,
    Skipped = 6
}
