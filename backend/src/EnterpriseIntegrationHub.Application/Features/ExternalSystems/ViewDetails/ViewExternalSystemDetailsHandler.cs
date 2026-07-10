using EnterpriseIntegrationHub.Application.Contracts.Responses;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.ViewDetails;

public sealed class ViewExternalSystemDetailsHandler
{
    private readonly IExternalSystemRepository _repository;

    public ViewExternalSystemDetailsHandler(IExternalSystemRepository repository)
    {
        _repository = repository;
    }

    public async Task<ExternalSystemSummary> Handle(ViewDetailsQuery query, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(query.Id,cancellationToken);

        if(item is null)
        {
            throw new KeyNotFoundException($"External system with ID {query.Id} not found.");
        }

        var details = new ExternalSystemSummary(
            Id: item.Id,
            Name: item.Name,
            Description: item.Description,
            Environment: item.Environment,
            Status: item.Status);

        return details;
    }
}
