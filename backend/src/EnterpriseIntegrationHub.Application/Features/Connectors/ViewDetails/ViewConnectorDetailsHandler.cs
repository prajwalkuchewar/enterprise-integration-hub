using EnterpriseIntegrationHub.Application.Contracts.Responses;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Application.Features.Connectors.ViewDetails;

public sealed class ViewConnectorDetailsHandler
{
    private readonly IConnectorRepository _repository;

    public ViewConnectorDetailsHandler(IConnectorRepository repository)
    {
        _repository = repository;
    }

    public async Task<ConnectorSummary> Handle(ViewConnectorDetailsQuery query, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(query.Id,cancellationToken);

        if(item is null)
        {
            throw new KeyNotFoundException($"External system with ID {query.Id} not found.");
        }

        var details = new ConnectorSummary(
            Id: item.Id,
            Name: item.Name,
            Description: item.Description,
            ExternalSystemId: item.ExternalSystemId,
            BaseUrl: item.BaseUrl,
            Protocol: item.Protocol,
            AuthenticationType: item.AuthenticationType,
            TimeoutSeconds: item.TimeoutSeconds,
            Status: item.Status);

        return details;
    }
}
