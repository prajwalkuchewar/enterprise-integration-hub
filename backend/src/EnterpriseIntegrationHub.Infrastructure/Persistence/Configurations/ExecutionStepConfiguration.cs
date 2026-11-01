using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class ExecutionStepConfiguration : IEntityTypeConfiguration<ExecutionStep>
{
    public void Configure(EntityTypeBuilder<ExecutionStep> builder)
    {
        builder.ToTable("ExecutionSteps");
        builder.HasKey(step => step.Id);
        builder.Property(step => step.Id).ValueGeneratedNever();
        builder.Property(step => step.DestinationBaseUrl).IsRequired().HasMaxLength(1000);
        builder.Property(step => step.Protocol).IsRequired().HasConversion<int>();
        builder.Property(step => step.AuthenticationType).IsRequired().HasConversion<int>();
        builder.Property(step => step.TimeoutSeconds).IsRequired();
        builder.Property(step => step.ExecutionOrder).IsRequired();
        builder.Property(step => step.Status).IsRequired().HasConversion<int>();
        builder.Property(step => step.NextAttemptAt).IsRequired(false);
        builder.HasIndex(step => new { step.IntegrationExecutionId, step.ExecutionOrder })
            .IsUnique().HasDatabaseName("IX_ExecutionSteps_ExecutionId_ExecutionOrder");
        builder.HasOne<WorkflowRevisionStep>().WithMany().HasForeignKey(step => step.WorkflowRevisionStepId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Connector>().WithMany().HasForeignKey(step => step.DestinationConnectorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(step => step.Attempts).WithOne().HasForeignKey(attempt => attempt.ExecutionStepId).OnDelete(DeleteBehavior.Cascade);
    }
}
