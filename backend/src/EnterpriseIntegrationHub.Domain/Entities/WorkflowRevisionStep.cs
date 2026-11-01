namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class WorkflowRevisionStep
{
    private WorkflowRevisionStep() { }

    public WorkflowRevisionStep(Guid workflowRevisionId, Guid destinationConnectorId, int executionOrder)
    {
        if (workflowRevisionId == Guid.Empty)
            throw new ArgumentException("WorkflowRevisionId must be provided.", nameof(workflowRevisionId));
        if (destinationConnectorId == Guid.Empty)
            throw new ArgumentException("DestinationConnectorId must be provided.", nameof(destinationConnectorId));
        if (executionOrder <= 0)
            throw new ArgumentOutOfRangeException(nameof(executionOrder));

        Id = Guid.NewGuid();
        WorkflowRevisionId = workflowRevisionId;
        DestinationConnectorId = destinationConnectorId;
        ExecutionOrder = executionOrder;
    }

    public Guid Id { get; private set; }
    public Guid WorkflowRevisionId { get; private set; }
    public Guid DestinationConnectorId { get; private set; }
    public int ExecutionOrder { get; private set; }
}
