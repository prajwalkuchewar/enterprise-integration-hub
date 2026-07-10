using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Interfaces
{
    public interface IConnectorRepository
    {
        Task<bool> ExistsAsync(Guid externalSystemId, ConnectorProtocol protocol, CancellationToken cancellationToken);

        Task AddAsync( Connector connector, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<Connector>> GetAllAsync(CancellationToken cancellationToken);

        Task<Connector?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    }
}
