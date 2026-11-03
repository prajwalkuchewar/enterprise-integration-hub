using System.Text.Json;

namespace EnterpriseIntegrationHub.Application.Features.Events.Ingest;

public sealed record IngestEventCommand(
    string IngestionKey,
    string EventId,
    string EventType,
    string SourceSystem,
    DateTimeOffset OccurredAt,
    string? CorrelationId,
    int SchemaVersion,
    JsonElement Payload);
