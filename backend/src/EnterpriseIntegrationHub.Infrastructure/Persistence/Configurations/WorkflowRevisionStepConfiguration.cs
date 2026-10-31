using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class WorkflowRevisionStepConfiguration : IEntityTypeConfiguration<WorkflowRevisionStep>
{
    public void Configure(EntityTypeBuilder<WorkflowRevisionStep> builder)
    {
        builder.ToTable("WorkflowRevisionSteps");
        builder.HasKey(step => step.Id);
        builder.Property(step => step.Id).ValueGeneratedNever();
        builder.Property(step => step.ExecutionOrder).IsRequired();
        builder.HasIndex(step => new { step.WorkflowRevisionId, step.ExecutionOrder })
            .IsUnique().HasDatabaseName("IX_WorkflowRevisionSteps_RevisionId_ExecutionOrder");
        builder.HasOne<Connector>().WithMany().HasForeignKey(step => step.DestinationConnectorId).OnDelete(DeleteBehavior.Restrict);
    }
}
