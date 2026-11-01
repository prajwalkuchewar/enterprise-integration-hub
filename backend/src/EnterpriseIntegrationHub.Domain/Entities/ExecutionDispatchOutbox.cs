namespace EnterpriseIntegrationHub.Domain.Entities;

public sealed class ExecutionDispatchOutbox
{
    private ExecutionDispatchOutbox() { }

    public ExecutionDispatchOutbox(Guid integrationExecutionId)
    {
        if (integrationExecutionId == Guid.Empty)
            throw new ArgumentException("IntegrationExecutionId must be provided.", nameof(integrationExecutionId));

        Id = Guid.NewGuid();
        IntegrationExecutionId = integrationExecutionId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid IntegrationExecutionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
}
