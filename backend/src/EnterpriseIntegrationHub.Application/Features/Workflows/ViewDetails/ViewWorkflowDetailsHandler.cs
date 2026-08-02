using EnterpriseIntegrationHub.Application.Contracts.Responses;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.Workflows.ViewDetails;

public sealed class ViewWorkflowDetailsHandler(IWorkflowRepository repository)
{
    public async Task<WorkflowDetailsResponseModel> Handle(ViewWorkflowDetailsQuery query, CancellationToken cancellationToken)
    {
        var workflow = await repository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow with ID {query.Id} not found.");

        return new WorkflowDetailsResponseModel(
            workflow.Id,
            workflow.Name,
            workflow.SourceConnectorId,
            workflow.TriggerEvent,
            workflow.Status,
            workflow.Steps.OrderBy(x => x.ExecutionOrder)
                .Select(x => new WorkflowStepResponseModel(x.DestinationConnectorId, x.ExecutionOrder))
                .ToList());
    }
}
