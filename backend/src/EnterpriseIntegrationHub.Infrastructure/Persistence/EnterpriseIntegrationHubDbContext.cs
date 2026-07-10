using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence;

public class EnterpriseIntegrationHubDbContext : DbContext
{
    public EnterpriseIntegrationHubDbContext(
        DbContextOptions<EnterpriseIntegrationHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExternalSystem> ExternalSystems => Set<ExternalSystem>();
    public DbSet<Connector> Connectors => Set<Connector>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnterpriseIntegrationHubDbContext).Assembly);
    }
}
