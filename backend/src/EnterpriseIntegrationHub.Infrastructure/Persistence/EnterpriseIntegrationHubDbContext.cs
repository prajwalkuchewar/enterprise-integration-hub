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
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowRevision> WorkflowRevisions => Set<WorkflowRevision>();
    public DbSet<WorkflowRevisionStep> WorkflowRevisionSteps => Set<WorkflowRevisionStep>();
    public DbSet<IntegrationExecution> IntegrationExecutions => Set<IntegrationExecution>();
    public DbSet<ExecutionStep> ExecutionSteps => Set<ExecutionStep>();
    public DbSet<ExecutionStepAttempt> ExecutionStepAttempts => Set<ExecutionStepAttempt>();
    public DbSet<ExecutionDispatchOutbox> ExecutionDispatchOutbox => Set<ExecutionDispatchOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnterpriseIntegrationHubDbContext).Assembly);
    }
}
