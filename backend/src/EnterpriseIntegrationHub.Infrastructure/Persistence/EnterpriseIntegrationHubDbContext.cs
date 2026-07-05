using EnterpriseIntegrationHub.Domain.Entities;
using EnterpriseIntegrationHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence
{
    public class EnterpriseIntegrationHubDbContext : DbContext
    {
        public EnterpriseIntegrationHubDbContext(
            DbContextOptions<EnterpriseIntegrationHubDbContext> options)
            : base(options)
        {
        }

        public DbSet<ExternalSystem> ExternalSystems => Set<ExternalSystem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ExternalSystemConfiguration());
        }

        private sealed class ExternalSystemConfiguration : IEntityTypeConfiguration<ExternalSystem>
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

                builder.HasIndex(x => x.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_ExternalSystems_Name");
            }
        }
    }
}
