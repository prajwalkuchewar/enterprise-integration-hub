using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Application.Interfaces
{
    public interface IExternalSystemRepository
    {
        Task<bool> ExistsAsync( string name, ExternalSystemEnvironment environment, CancellationToken cancellationToken);

        Task AddAsync( ExternalSystem externalSystem, CancellationToken cancellationToken);
    }
}
