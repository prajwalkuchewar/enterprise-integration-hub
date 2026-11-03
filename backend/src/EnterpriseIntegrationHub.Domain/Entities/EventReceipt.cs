namespace EnterpriseIntegrationHub.Domain.Entities;

/// <summary>
/// Durable acceptance record for an inbound event. It is written in the same transaction as the
/// execution it created, so an accepted event always has exactly one receipt and one execution.
/// </summary>
public sealed class EventReceipt
{
    private EventReceipt() { }

    public EventReceipt(IntegrationExecution execution)
    {
        ArgumentNullException.ThrowIfNull(execution);

        Id = Guid.NewGuid();
        SourceConnectorId = execution.SourceConnectorId;
        SourceEventId = execution.SourceEventId;
        EventFingerprint = execution.EventFingerprint;
        IntegrationExecutionId = execution.Id;
        ReceivedAt = execution.ReceivedAt;
    }

    public Guid Id { get; private set; }
    public Guid SourceConnectorId { get; private set; }
    public string SourceEventId { get; private set; } = string.Empty;
    public string EventFingerprint { get; private set; } = string.Empty;
    public Guid IntegrationExecutionId { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
}
