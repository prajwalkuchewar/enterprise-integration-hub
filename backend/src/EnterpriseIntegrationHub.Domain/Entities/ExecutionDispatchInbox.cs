namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class ExecutionDispatchInbox
{
    private ExecutionDispatchInbox() { }

    public ExecutionDispatchInbox(Guid dispatchId, Guid executionId, DateTimeOffset receivedAt)
    {
        if (dispatchId == Guid.Empty)
            throw new ArgumentException("DispatchId must be provided.", nameof(dispatchId));
        if (executionId == Guid.Empty)
            throw new ArgumentException("ExecutionId must be provided.", nameof(executionId));

        DispatchId = dispatchId;
        IntegrationExecutionId = executionId;
        ReceivedAt = receivedAt;
    }

    public Guid DispatchId { get; private set; }
    public Guid IntegrationExecutionId { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
}
