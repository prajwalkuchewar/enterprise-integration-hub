using EnterpriseIntegrationHub.Application.Contracts.Responses;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Browse;

public sealed class BrowseHandler
{
    private readonly IExternalSystemRepository _repository;

    public BrowseHandler(IExternalSystemRepository repository)
    {
        _repository = repository;
    }

    public async Task<ExternalSystemsResponseModel> Handle(BrowseQuery query, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(cancellationToken);

        var summaries = items
            .Select(x => new ExternalSystemSummary(
            Id: x.Id,
            Name: x.Name,
            Description: x.Description,
            Environment: x.Environment,
            Status: x.Status))
            .ToList();

        return new ExternalSystemsResponseModel(
            summaries,
            summaries.Count);
    }
}
