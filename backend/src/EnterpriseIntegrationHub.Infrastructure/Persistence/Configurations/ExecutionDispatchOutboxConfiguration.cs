using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class ExecutionDispatchOutboxConfiguration : IEntityTypeConfiguration<ExecutionDispatchOutbox>
{
    public void Configure(EntityTypeBuilder<ExecutionDispatchOutbox> builder)
    {
        builder.ToTable("ExecutionDispatchOutbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.CreatedAt).IsRequired();
        builder.Property(message => message.DispatchedAt).IsRequired(false);
        builder.HasIndex(message => message.IntegrationExecutionId).IsUnique()
            .HasDatabaseName("IX_ExecutionDispatchOutbox_ExecutionId");
        builder.HasOne<IntegrationExecution>().WithMany().HasForeignKey(message => message.IntegrationExecutionId).OnDelete(DeleteBehavior.Cascade);
    }
}
