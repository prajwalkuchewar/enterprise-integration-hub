using EnterpriseIntegrationHub.Application.Features.Workflows.Resolve;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;

namespace EnterpriseIntegrationHub.Application.Features.Executions.Create;

public sealed class CreateWorkflowExecutionHandler(
    ResolveWorkflowHandler workflowResolver,
    IWorkflowRepository workflowRepository,
    IIntegrationExecutionRepository executionRepository)
{
    public async Task<CreateWorkflowExecutionResult> Handle(CreateWorkflowExecutionCommand command, CancellationToken cancellationToken)
    {
        Validate(command);

        var existing = await executionRepository.GetBySourceEventAsync(command.SourceConnectorId, command.SourceEventId.Trim(), cancellationToken);
        if (existing is not null)
            return ExistingResult(existing, command.EventFingerprint);

        var resolved = await workflowResolver.Handle(
            new ResolveWorkflowQuery(command.SourceConnectorId, command.TriggerEvent), cancellationToken);
        var revision = await workflowRepository.GetRevisionByIdAsync(resolved.WorkflowRevisionId, cancellationToken)
            ?? throw new InvalidOperationException($"Workflow revision {resolved.WorkflowRevisionId} was not found after resolution.");

        var steps = resolved.Steps.Select(step => new ExecutionStepSnapshot(
            step.WorkflowRevisionStepId,
            step.DestinationConnectorId,
            step.BaseUrl,
            step.Protocol,
            step.AuthenticationType,
            step.TimeoutSeconds,
            step.ExecutionOrder));
        var execution = new IntegrationExecution(
            revision,
            command.SourceConnectorId,
            command.SourceEventId,
            command.OccurredAt,
            command.CorrelationId,
            command.EventFingerprint,
            steps);
        var createdOrExisting = await executionRepository.CreateIfAbsentAsync(
            execution,
            new ExecutionDispatchOutbox(execution.Id),
            cancellationToken);

        return createdOrExisting.Id == execution.Id
            ? new CreateWorkflowExecutionResult(execution.Id, execution.Status, false)
            : ExistingResult(createdOrExisting, command.EventFingerprint);
    }

    private static CreateWorkflowExecutionResult ExistingResult(IntegrationExecution existing, string eventFingerprint)
    {
        if (!string.Equals(existing.EventFingerprint, eventFingerprint.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A source event with this ID was already accepted with different content.");

        return new CreateWorkflowExecutionResult(existing.Id, existing.Status, true);
    }

    private static void Validate(CreateWorkflowExecutionCommand command)
    {
        if (command.SourceConnectorId == Guid.Empty)
            throw new ArgumentException("SourceConnectorId must be provided.", nameof(command.SourceConnectorId));
        if (string.IsNullOrWhiteSpace(command.SourceEventId) || command.SourceEventId.Trim().Length > 200)
            throw new ArgumentException("SourceEventId is required and must not exceed 200 characters.", nameof(command.SourceEventId));
        if (string.IsNullOrWhiteSpace(command.TriggerEvent) || command.TriggerEvent.Trim().Length > 200)
            throw new ArgumentException("TriggerEvent is required and must not exceed 200 characters.", nameof(command.TriggerEvent));
        if (string.IsNullOrWhiteSpace(command.EventFingerprint) || command.EventFingerprint.Length != 64 ||
            command.EventFingerprint.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("EventFingerprint must be a 64-character SHA-256 hex value.", nameof(command.EventFingerprint));
        if (command.CorrelationId?.Length > 200)
            throw new ArgumentException("CorrelationId must not exceed 200 characters.", nameof(command.CorrelationId));
    }
}
