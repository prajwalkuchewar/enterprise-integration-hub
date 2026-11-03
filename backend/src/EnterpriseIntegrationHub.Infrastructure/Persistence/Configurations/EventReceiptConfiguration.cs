using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class EventReceiptConfiguration : IEntityTypeConfiguration<EventReceipt>
{
    public void Configure(EntityTypeBuilder<EventReceipt> builder)
    {
        builder.ToTable("EventReceipts");
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.Id).ValueGeneratedNever();
        builder.Property(receipt => receipt.SourceEventId).IsRequired().HasMaxLength(200).UseCollation("SQL_Latin1_General_CP1_CS_AS");
        builder.Property(receipt => receipt.EventFingerprint).IsRequired().HasMaxLength(64);
        builder.Property(receipt => receipt.ReceivedAt).IsRequired();
        builder.HasIndex(receipt => new { receipt.SourceConnectorId, receipt.SourceEventId })
            .IsUnique().HasDatabaseName("IX_EventReceipts_SourceConnectorId_SourceEventId");
        builder.HasIndex(receipt => receipt.IntegrationExecutionId)
            .IsUnique().HasDatabaseName("IX_EventReceipts_IntegrationExecutionId");
        builder.HasOne<Connector>().WithMany().HasForeignKey(receipt => receipt.SourceConnectorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IntegrationExecution>().WithMany().HasForeignKey(receipt => receipt.IntegrationExecutionId).OnDelete(DeleteBehavior.Cascade);
    }
}
