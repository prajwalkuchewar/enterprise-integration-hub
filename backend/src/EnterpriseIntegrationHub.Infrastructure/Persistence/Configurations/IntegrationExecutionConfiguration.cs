using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class IntegrationExecutionConfiguration : IEntityTypeConfiguration<IntegrationExecution>
{
    public void Configure(EntityTypeBuilder<IntegrationExecution> builder)
    {
        builder.ToTable("IntegrationExecutions");
        builder.HasKey(execution => execution.Id);
        builder.Property(execution => execution.Id).ValueGeneratedNever();
        builder.Property(execution => execution.SourceEventId).IsRequired().HasMaxLength(200).UseCollation("SQL_Latin1_General_CP1_CS_AS");
        builder.Property(execution => execution.TriggerEvent).IsRequired().HasMaxLength(200);
        builder.Property(execution => execution.CorrelationId).HasMaxLength(200);
        builder.Property(execution => execution.EventFingerprint).IsRequired().HasMaxLength(64);
        builder.Property(execution => execution.Status).IsRequired().HasConversion<int>();
        builder.Property(execution => execution.OccurredAt).IsRequired();
        builder.Property(execution => execution.ReceivedAt).IsRequired();
        builder.Property(execution => execution.StartedAt).IsRequired(false);
        builder.Property(execution => execution.CompletedAt).IsRequired(false);
        builder.Property(execution => execution.CreatedAt).IsRequired();
        builder.Property(execution => execution.UpdatedAt).IsRequired(false);
        builder.Property(execution => execution.RowVersion).IsRowVersion();
        builder.HasIndex(execution => new { execution.SourceConnectorId, execution.SourceEventId })
            .IsUnique().HasDatabaseName("IX_IntegrationExecutions_SourceConnectorId_SourceEventId");
        builder.HasOne<Connector>().WithMany().HasForeignKey(execution => execution.SourceConnectorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowRevision>().WithMany().HasForeignKey(execution => execution.WorkflowRevisionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(execution => execution.Steps).WithOne().HasForeignKey(step => step.IntegrationExecutionId).OnDelete(DeleteBehavior.Cascade);
    }
}
