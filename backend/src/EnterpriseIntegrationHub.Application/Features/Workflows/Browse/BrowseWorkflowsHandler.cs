using EnterpriseIntegrationHub.Application.Contracts.Responses;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.Workflows.Browse;

public sealed class BrowseWorkflowsHandler(IWorkflowRepository repository)
{
    public async Task<WorkflowsResponseModel> Handle(BrowseWorkflowsQuery query, CancellationToken cancellationToken)
    {
        var workflows = await repository.GetAllAsync(cancellationToken);
        var summaries = workflows.Select(x => new WorkflowSummary(x.Id, x.Name, x.SourceConnectorId, x.TriggerEvent, x.Status, x.Steps.Count)).ToList();
        return new WorkflowsResponseModel(summaries, summaries.Count);
    }
}
