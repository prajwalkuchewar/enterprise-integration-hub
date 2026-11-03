namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public enum DestinationInvocationResult
{
    Succeeded,
    TransientFailure,
    PermanentFailure,
    InvalidConfiguration
}
