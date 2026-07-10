using EnterpriseIntegrationHub.Application.Contracts.Responses;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.ExternalSystems.Browse;

public sealed class BrowseExternalSystemsHandler
{
  private readonly IExternalSystemRepository _repository;

  public BrowseExternalSystemsHandler(IExternalSystemRepository repository)
  {
    _repository = repository;
  }

  public async Task<ExternalSystemsResponseModel> Handle(BrowseExternalSystemsQuery query, CancellationToken cancellationToken)
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
