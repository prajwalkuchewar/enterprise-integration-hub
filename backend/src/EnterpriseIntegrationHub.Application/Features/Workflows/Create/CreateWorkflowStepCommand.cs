namespace EnterpriseIntegrationHub.Application.Features.Workflows.Create;

public sealed record CreateWorkflowStepCommand(Guid DestinationConnectorId, int ExecutionOrder);
