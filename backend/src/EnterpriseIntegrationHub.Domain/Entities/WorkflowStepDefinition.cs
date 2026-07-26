namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed record WorkflowStepDefinition(Guid DestinationConnectorId, int ExecutionOrder);
