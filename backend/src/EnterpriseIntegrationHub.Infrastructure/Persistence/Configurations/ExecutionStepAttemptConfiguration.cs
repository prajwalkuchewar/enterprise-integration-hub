using EnterpriseIntegrationHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseIntegrationHub.Infrastructure.Persistence.Configurations;

public sealed class ExecutionStepAttemptConfiguration : IEntityTypeConfiguration<ExecutionStepAttempt>
{
    public void Configure(EntityTypeBuilder<ExecutionStepAttempt> builder)
    {
        builder.ToTable("ExecutionStepAttempts");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Id).ValueGeneratedNever();
        builder.Property(attempt => attempt.AttemptNumber).IsRequired();
        builder.Property(attempt => attempt.Status).IsRequired().HasConversion<int>();
        builder.Property(attempt => attempt.StartedAt).IsRequired();
        builder.Property(attempt => attempt.CompletedAt).IsRequired(false);
        builder.Property(attempt => attempt.FailureCode).HasMaxLength(100);
        builder.Property(attempt => attempt.FailureMessage).HasMaxLength(2000);
        builder.HasIndex(attempt => new { attempt.ExecutionStepId, attempt.AttemptNumber })
            .IsUnique().HasDatabaseName("IX_ExecutionStepAttempts_StepId_AttemptNumber");
    }
}
