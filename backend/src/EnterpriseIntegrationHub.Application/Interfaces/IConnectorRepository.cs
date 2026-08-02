using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Interfaces
{
    public interface IConnectorRepository
    {
        Task<bool> ExistsAsync(Guid externalSystemId, string name, CancellationToken cancellationToken, Guid? excludedConnectorId = null);

        Task AddAsync(Connector connector, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<Connector>> GetAllAsync(CancellationToken cancellationToken);

        Task<Connector?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task UpdateAsync(Connector connector, CancellationToken cancellationToken);
    }
}
