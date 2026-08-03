namespace EnterpriseIntegrationHub.Application.Features.Workflows.Update;

public sealed record UpdateWorkflowStepCommand(Guid DestinationConnectorId, int ExecutionOrder);

public sealed record UpdateWorkflowCommand(string Name, Guid SourceConnectorId, IReadOnlyCollection<UpdateWorkflowStepCommand> Steps, string TriggerEvent);
