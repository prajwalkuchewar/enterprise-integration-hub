using EnterpriseIntegrationHub.Application.Contracts.Responses;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.Browse;

public sealed class BrowseConnectorsHandler
{
    private readonly IConnectorRepository _repository;

    public BrowseConnectorsHandler(IConnectorRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConnectorsResponseModel> Handle(BrowseQuery query, CancellationToken cancellationToken)
    {
        var items = await _repository.GetAllAsync(cancellationToken);

        var summaries = items
            .OrderBy(x => x.Name)
            .Select(x => new ConnectorSummary(
            Id: x.Id,
            Name: x.Name,
            Description: x.Description,
            ExternalSystemId: x.ExternalSystemId,
            BaseUrl: x.BaseUrl,
            Protocol: x.Protocol,
            AuthenticationType: x.AuthenticationType,
            TimeoutSeconds: x.TimeoutSeconds,
            Status: x.Status))
            .ToList();

        return new ConnectorsResponseModel(
            summaries,
            summaries.Count);
    }
}
