using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Features.Workflows.Resolve;

public sealed class ResolveWorkflowHandler(IWorkflowRepository workflowRepository, IConnectorRepository connectorRepository)
{
    public async Task<ResolvedWorkflowConfiguration> Handle(ResolveWorkflowQuery query, CancellationToken cancellationToken)
    {
        if (query.SourceConnectorId == Guid.Empty)
            throw new ArgumentException("SourceConnectorId must be provided.", nameof(query.SourceConnectorId));
        if (string.IsNullOrWhiteSpace(query.TriggerEvent))
            throw new ArgumentException("TriggerEvent is required.", nameof(query.TriggerEvent));

        var eventType = query.TriggerEvent.Trim();
        var candidates = await workflowRepository.GetActiveRevisionsForSourceConnectorAsync(query.SourceConnectorId, cancellationToken);
        var matches = candidates
            .Where(workflow => string.Equals(workflow.TriggerEvent.Trim(), eventType, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
            throw new KeyNotFoundException($"No active workflow matches trigger event '{eventType}' for source connector {query.SourceConnectorId}.");
        if (matches.Length > 1)
            throw new InvalidOperationException($"More than one active workflow matches trigger event '{eventType}' for source connector {query.SourceConnectorId}.");

        var revision = matches[0];
        await EnsureActiveConnector(revision.SourceConnectorId, "Source", cancellationToken);
        var resolvedSteps = new List<ResolvedWorkflowStep>();
        foreach (var step in revision.Steps.OrderBy(step => step.ExecutionOrder))
        {
            var connector = await EnsureActiveConnector(step.DestinationConnectorId, "Destination", cancellationToken);
            resolvedSteps.Add(new ResolvedWorkflowStep(
                step.Id,
                connector.Id,
                connector.BaseUrl,
                connector.Protocol,
                connector.AuthenticationType,
                connector.TimeoutSeconds,
                step.ExecutionOrder));
        }

        return new ResolvedWorkflowConfiguration(
            revision.Id,
            revision.WorkflowId,
            revision.RevisionNumber,
            revision.Name,
            revision.SourceConnectorId,
            revision.TriggerEvent,
            resolvedSteps);
    }

    private async Task<Connector> EnsureActiveConnector(Guid connectorId, string role, CancellationToken cancellationToken)
    {
        var connector = await connectorRepository.GetByIdAsync(connectorId, cancellationToken)
            ?? throw new KeyNotFoundException($"{role} connector with ID {connectorId} was not found.");
        if (connector.Status != ConnectorStatus.Active)
            throw new InvalidOperationException($"The matching workflow cannot be resolved because its {role.ToLowerInvariant()} connector {connectorId} is not active.");
        return connector;
    }
}
