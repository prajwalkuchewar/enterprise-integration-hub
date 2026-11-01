using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed record ExecutionStepSnapshot(
    Guid WorkflowRevisionStepId,
    Guid DestinationConnectorId,
    string BaseUrl,
    ConnectorProtocol Protocol,
    ConnectorAuthenticationType AuthenticationType,
    int TimeoutSeconds,
    int ExecutionOrder);
