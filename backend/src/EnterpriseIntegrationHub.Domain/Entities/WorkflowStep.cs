namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class WorkflowStep
{
    private WorkflowStep() { }

    public WorkflowStep(Guid workflowId, Guid destinationConnectorId, int executionOrder)
    {
        WorkflowId = workflowId;
        DestinationConnectorId = destinationConnectorId;
        ExecutionOrder = executionOrder;
    }

    public Guid WorkflowId { get; private set; }
    public Guid DestinationConnectorId { get; private set; }
    public int ExecutionOrder { get; private set; }
}
