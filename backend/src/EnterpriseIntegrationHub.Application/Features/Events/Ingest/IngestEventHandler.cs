using EnterpriseIntegrationHub.Application.Features.Executions.Create;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Application.Security;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using System.Text.Json;

namespace EnterpriseIntegrationHub.Application.Features.Events.Ingest;

/// <summary>
/// Authenticates an inbound event by its connector ingestion key, validates the envelope,
/// and records it as a durable execution. Workflow resolution and dispatch happen downstream.
/// </summary>
public sealed class IngestEventHandler(
    IConnectorRepository connectorRepository,
    IExternalSystemRepository externalSystemRepository,
    CreateWorkflowExecutionHandler createWorkflowExecution)
{
    public async Task<CreateWorkflowExecutionResult> Handle(IngestEventCommand command, CancellationToken cancellationToken)
    {
        Validate(command);

        var connector = await AuthenticateAsync(command.IngestionKey, cancellationToken);
        await EnsureSourceSystemMatchesAsync(connector, command.SourceSystem.Trim(), cancellationToken);

        if (connector.Status != ConnectorStatus.Active)
            throw new InvalidOperationException($"Connector with ID {connector.Id} is not active and cannot accept events.");

        var occurredAt = command.OccurredAt.ToUniversalTime();
        var fingerprint = EventFingerprint.Compute(command.EventType.Trim(), occurredAt, command.SchemaVersion, command.Payload);

        return await createWorkflowExecution.Handle(
            new CreateWorkflowExecutionCommand(
                connector.Id,
                command.EventId.Trim(),
                command.EventType.Trim(),
                occurredAt,
                string.IsNullOrWhiteSpace(command.CorrelationId) ? null : command.CorrelationId.Trim(),
                fingerprint),
            cancellationToken);
    }

    private async Task<Connector> AuthenticateAsync(string ingestionKey, CancellationToken cancellationToken)
    {
        var connector = await connectorRepository.GetByIngestionKeyHashAsync(IngestionKeys.Hash(ingestionKey), cancellationToken);
        return connector ?? throw new UnauthorizedAccessException("The ingestion key is not valid.");
    }

    private async Task EnsureSourceSystemMatchesAsync(Connector connector, string sourceSystem, CancellationToken cancellationToken)
    {
        var externalSystem = await externalSystemRepository.GetByIdAsync(connector.ExternalSystemId, cancellationToken)
            ?? throw new KeyNotFoundException($"External system with ID {connector.ExternalSystemId} not found.");

        if (!string.Equals(externalSystem.Name, sourceSystem, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException($"The ingestion key is not authorized to submit events for source system '{sourceSystem}'.");
    }

    private static void Validate(IngestEventCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.IngestionKey))
            throw new UnauthorizedAccessException("An ingestion key is required.");
        if (string.IsNullOrWhiteSpace(command.EventId) || command.EventId.Trim().Length > 200)
            throw new ArgumentException("EventId is required and must not exceed 200 characters.", nameof(command.EventId));
        if (string.IsNullOrWhiteSpace(command.EventType) || command.EventType.Trim().Length > 200)
            throw new ArgumentException("EventType is required and must not exceed 200 characters.", nameof(command.EventType));
        if (string.IsNullOrWhiteSpace(command.SourceSystem) || command.SourceSystem.Trim().Length > 200)
            throw new ArgumentException("SourceSystem is required and must not exceed 200 characters.", nameof(command.SourceSystem));
        if (command.OccurredAt == default)
            throw new ArgumentException("OccurredAt is required.", nameof(command.OccurredAt));
        if (command.CorrelationId?.Trim().Length > 200)
            throw new ArgumentException("CorrelationId must not exceed 200 characters.", nameof(command.CorrelationId));
        if (command.SchemaVersion < 1)
            throw new ArgumentException("SchemaVersion must be a positive integer.", nameof(command.SchemaVersion));
        if (command.Payload.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Payload must be a JSON object.", nameof(command.Payload));
    }
}
