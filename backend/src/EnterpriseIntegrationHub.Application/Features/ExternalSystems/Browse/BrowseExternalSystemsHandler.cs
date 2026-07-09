using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Application.Contracts.Responses;

public sealed class BrowseExternalSystemHandler
{
    private readonly IExternalSystemRepository _repository;

    public BrowseExternalSystemHandler(IExternalSystemRepository repository)
    {
        _repository = repository;
    }

    public async Task<BrowseExternalSystemsResponseModel> Handle(CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(cancellationToken);

        if (items== null || !items.Any())
        {
            return new BrowseExternalSystemsResponseModel(Array.Empty<ExternalSystemSummary>(), TotalCount: 0);

        }

        var summaries = items
            .Select(x => new ExternalSystemSummary(
            Id: x.Id,
            Name: x.Name,
            Description: x.Description,
            Environment: x.Environment,
            Status: x.Status))
            .ToList();

        return new BrowseExternalSystemsResponseModel(
            summaries,
            summaries.Count);
    }
}
