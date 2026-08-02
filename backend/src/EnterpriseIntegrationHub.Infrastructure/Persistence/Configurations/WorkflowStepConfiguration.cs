using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WorkflowSteps");
        builder.HasKey(x => new { x.WorkflowId, x.ExecutionOrder });
        builder.Property(x => x.ExecutionOrder).IsRequired();
        // Relationship to parent Workflow (navigation property 'Steps' on Workflow)
        builder.HasOne<Workflow>()
               .WithMany(w => w.Steps)
               .HasForeignKey(x => x.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);

        // Relationship to destination connector
        builder.HasOne<Connector>()
               .WithMany()
               .HasForeignKey(x => x.DestinationConnectorId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.DestinationConnectorId);
    }
}
