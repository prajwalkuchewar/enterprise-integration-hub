namespace EnterpriseIntegrationHub.Application.Features.Workflows.Create;

public sealed record CreateWorkflowStepCommand(Guid DestinationConnectorId, int ExecutionOrder);

public sealed record CreateWorkflowCommand(
    string Name,
    Guid SourceConnectorId,
    IReadOnlyCollection<CreateWorkflowStepCommand> Steps,
    string TriggerEvent);
