using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class ExecutionDispatchInboxConfiguration : IEntityTypeConfiguration<ExecutionDispatchInbox>
{
    public void Configure(EntityTypeBuilder<ExecutionDispatchInbox> builder)
    {
        builder.ToTable("ExecutionDispatchInbox");
        builder.HasKey(item => item.DispatchId);
        builder.Property(item => item.DispatchId).ValueGeneratedNever();
        builder.Property(item => item.ReceivedAt).IsRequired();
        builder.HasIndex(item => item.IntegrationExecutionId).IsUnique();
        builder.HasIndex(item => item.LeaseToken).IsUnique().HasFilter("[LeaseToken] IS NOT NULL");
        builder.HasIndex(item => new { item.CompletedAt, item.AvailableAt, item.LeaseExpiresAt });
        builder.HasOne<ExecutionDispatchOutbox>().WithMany().HasForeignKey(item => item.DispatchId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<IntegrationExecution>().WithMany().HasForeignKey(item => item.IntegrationExecutionId).OnDelete(DeleteBehavior.NoAction);
    }
}
