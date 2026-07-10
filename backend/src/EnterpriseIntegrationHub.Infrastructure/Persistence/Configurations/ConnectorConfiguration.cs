using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class ConnectorConfiguration : IEntityTypeConfiguration<Connector>
{
    public void Configure(EntityTypeBuilder<Connector> builder)
    {
        builder.ToTable("Connectors");

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

        builder.HasOne<ExternalSystem>()
            .WithMany()
            .HasForeignKey(x => x.ExternalSystemId)
            .IsRequired();

        builder.Property(x => x.BaseUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Protocol)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.AuthenticationType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.TimeoutSeconds)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(x => new { x.Name, x.ExternalSystemId })
            .IsUnique()
            .HasDatabaseName("IX_Connectors_Name_ExternalSystemId");
    }
}
