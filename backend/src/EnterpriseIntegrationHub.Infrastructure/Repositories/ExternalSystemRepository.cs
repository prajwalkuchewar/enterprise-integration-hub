using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Repositories;

public sealed class ExternalSystemRepository : IExternalSystemRepository
{
    private readonly EnterpriseIntegrationHubDbContext _context;

    public ExternalSystemRepository(EnterpriseIntegrationHubDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(
        string name,
        ExternalSystemEnvironment environment,
        CancellationToken cancellationToken)
    {
        return await _context.ExternalSystems
            .AnyAsync(x =>
                x.Environment == environment &&
                x.Name == name,
                cancellationToken);
    }

    public async Task AddAsync(
        ExternalSystem externalSystem,
        CancellationToken cancellationToken)
    {
        _context.ExternalSystems.Add(externalSystem);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExternalSystem>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.ExternalSystems
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    }

    public async Task<ExternalSystem?> GetDetailsAsync(Guid Id, CancellationToken cancellationToken)
    {
        return await _context.ExternalSystems
            .AsNoTracking()
            .Where(x => x.Id == Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

}
