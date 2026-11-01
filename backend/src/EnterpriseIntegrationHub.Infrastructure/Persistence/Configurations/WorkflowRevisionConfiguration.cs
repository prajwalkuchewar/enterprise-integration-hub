using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class WorkflowRevisionConfiguration : IEntityTypeConfiguration<WorkflowRevision>
{
    public void Configure(EntityTypeBuilder<WorkflowRevision> builder)
    {
        builder.ToTable("WorkflowRevisions");
        builder.HasKey(revision => revision.Id);
        builder.Property(revision => revision.Id).ValueGeneratedNever();
        builder.Property(revision => revision.RevisionNumber).IsRequired();
        builder.Property(revision => revision.Name).IsRequired().HasMaxLength(200).UseCollation("SQL_Latin1_General_CP1_CI_AS");
        builder.Property(revision => revision.TriggerEvent).IsRequired().HasMaxLength(200);
        builder.Property(revision => revision.PublishedAt).IsRequired();
        builder.Property(revision => revision.CreatedAt).IsRequired();
        builder.Property(revision => revision.UpdatedAt).IsRequired(false);
        builder.Property(revision => revision.RowVersion).IsRowVersion();
        builder.HasIndex(revision => new { revision.WorkflowId, revision.RevisionNumber })
            .IsUnique().HasDatabaseName("IX_WorkflowRevisions_WorkflowId_RevisionNumber");
        builder.HasOne<Workflow>().WithMany().HasForeignKey(revision => revision.WorkflowId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Connector>().WithMany().HasForeignKey(revision => revision.SourceConnectorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(revision => revision.Steps).WithOne().HasForeignKey(step => step.WorkflowRevisionId).OnDelete(DeleteBehavior.Cascade);
    }
}
