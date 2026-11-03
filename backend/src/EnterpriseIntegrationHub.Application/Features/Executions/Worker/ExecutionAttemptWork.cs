using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public sealed record ExecutionAttemptWork(
    Guid ExecutionId,
    Guid StepId,
    Guid AttemptId,
    int AttemptNumber,
    int ExecutionOrder,
    Guid DestinationConnectorId,
    string DestinationBaseUrl,
    ConnectorProtocol Protocol,
    ConnectorAuthenticationType AuthenticationType,
    int TimeoutSeconds,
    string? CorrelationId,
    string PayloadJson,
    ConnectorHttpMethod HttpMethod = ConnectorHttpMethod.Post)
{
    public string IdempotencyKey => StepId.ToString("D");

    public override string ToString() => $"Execution {ExecutionId}, step {StepId}, attempt {AttemptId}";
}
