using System.Text.Json;

namespace EnterpriseIntegrationHub.Api.Contracts.Requests;

/// <summary>Request envelope for submitting an event through an authenticated connector.</summary>
public sealed class IngestEventRequest
{
    /// <summary>Gets the source-assigned event identifier, unique per source connector.</summary>
    public string EventId { get; init; } = string.Empty;

    /// <summary>Gets the event type, matched against workflow trigger events.</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>Gets the name of the registered external system that produced the event.</summary>
    public string SourceSystem { get; init; } = string.Empty;

    /// <summary>Gets the time the event occurred, in ISO 8601 format.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Gets an optional correlation identifier for tracing across systems.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Gets the version of the payload schema.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Gets the JSON object payload.</summary>
    public JsonElement Payload { get; init; }
}
