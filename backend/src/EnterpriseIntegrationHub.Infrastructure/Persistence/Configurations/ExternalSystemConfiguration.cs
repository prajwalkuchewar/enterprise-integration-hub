using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class ExternalSystemConfiguration : IEntityTypeConfiguration<ExternalSystem>
{
    public void Configure(EntityTypeBuilder<ExternalSystem> builder)
    {
        builder.ToTable("ExternalSystems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200)
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Environment)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(x => new { x.Name, x.Environment })
            .IsUnique()
            .HasDatabaseName("IX_ExternalSystems_Name_Environment");
    }
}
