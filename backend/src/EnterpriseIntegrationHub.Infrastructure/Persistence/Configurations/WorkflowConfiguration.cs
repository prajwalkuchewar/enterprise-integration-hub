using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        builder.ToTable("Workflows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200).UseCollation("SQL_Latin1_General_CP1_CI_AS");
        builder.Property(x => x.TriggerEvent).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("IX_Workflows_Name");
        builder.HasOne<Connector>().WithMany().HasForeignKey(x => x.SourceConnectorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Steps).WithOne().HasForeignKey(x => x.WorkflowId);
    }
}
