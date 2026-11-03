using System.Text.Json;

namespace EnterpriseIntegrationHub.Domain.Entities;

/// <summary>
/// Durable acceptance record for an inbound event. It is written in the same transaction as the
/// execution it created, so an accepted event always has exactly one receipt and one execution.
/// </summary>
public sealed class EventReceipt
{
    private EventReceipt() { }

    public EventReceipt(IntegrationExecution execution, int schemaVersion, JsonElement payload)
    {
        ArgumentNullException.ThrowIfNull(execution);
        if (schemaVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(schemaVersion), "SchemaVersion must be a positive integer.");
        if (payload.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Payload must be a JSON object.", nameof(payload));

        Id = Guid.NewGuid();
        SourceConnectorId = execution.SourceConnectorId;
        SourceEventId = execution.SourceEventId;
        EventFingerprint = execution.EventFingerprint;
        IntegrationExecutionId = execution.Id;
        ReceivedAt = execution.ReceivedAt;
        SchemaVersion = schemaVersion;
        PayloadJson = payload.GetRawText();
    }

    public Guid Id { get; private set; }
    public Guid SourceConnectorId { get; private set; }
    public string SourceEventId { get; private set; } = string.Empty;
    public string EventFingerprint { get; private set; } = string.Empty;
    public Guid IntegrationExecutionId { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public int? SchemaVersion { get; private set; }
    public string? PayloadJson { get; private set; }
}
