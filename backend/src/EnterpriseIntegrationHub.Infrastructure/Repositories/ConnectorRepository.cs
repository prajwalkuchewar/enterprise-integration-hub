using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Repositories;

public sealed class ConnectorRepository : IConnectorRepository
{
    private readonly EnterpriseIntegrationHubDbContext _context;

    public ConnectorRepository(EnterpriseIntegrationHubDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(
        Guid externalSystemId,
        string name,
        ConnectorProtocol protocol,
        CancellationToken cancellationToken)
    {
        return await _context.Connectors
            .AnyAsync(x =>
                x.ExternalSystemId == externalSystemId &&
                x.Name == name &&
                x.Protocol == protocol &&
                x.Status != ConnectorStatus.Inactive,
                cancellationToken);
    }

    public async Task AddAsync(
        Connector connector,
        CancellationToken cancellationToken)
    {
        _context.Connectors.Add(connector);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Connector>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Connectors
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    }

    public async Task<Connector?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Connectors
            .AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

}
