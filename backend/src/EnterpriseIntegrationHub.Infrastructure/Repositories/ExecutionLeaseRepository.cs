using EnterpriseIntegrationHub.Application.Features.Executions.Worker;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Enums;
using EnterpriseIntegrationHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseIntegrationHub.Infrastructure.Repositories;

public sealed class ExecutionLeaseRepository(EnterpriseIntegrationHubDbContext context) : IExecutionLeaseRepository
{
    public async Task<ExecutionLease?> ClaimAsync(int leaseSeconds, CancellationToken cancellationToken)
    {
        ValidateDuration(leaseSeconds);
        var token = Guid.NewGuid();

        // The token also identifies a prior successful claim if EF retries after an ambiguous SQL response.
        var claimed = await context.ExecutionDispatchInbox.FromSqlInterpolated($"""
            IF EXISTS (
                SELECT 1 FROM [ExecutionDispatchInbox]
                WHERE [LeaseToken] = {token} AND [CompletedAt] IS NULL
                  AND [LeaseExpiresAt] > SYSDATETIMEOFFSET()
            )
                SELECT * FROM [ExecutionDispatchInbox] WHERE [LeaseToken] = {token};
            ELSE
            BEGIN
                ;WITH candidate AS (
                    SELECT TOP (1) * FROM [ExecutionDispatchInbox] WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
                    WHERE [CompletedAt] IS NULL
                      AND ([AvailableAt] IS NULL OR [AvailableAt] <= SYSDATETIMEOFFSET())
                      AND ([LeaseExpiresAt] IS NULL OR [LeaseExpiresAt] <= SYSDATETIMEOFFSET())
                    ORDER BY [ReceivedAt], [DispatchId]
                )
                UPDATE candidate
                SET [LeaseToken] = {token},
                    [LeaseExpiresAt] = DATEADD(second, {leaseSeconds}, SYSDATETIMEOFFSET())
                OUTPUT inserted.*;
            END
            """).AsNoTracking().ToListAsync(cancellationToken);

        var item = claimed.SingleOrDefault();
        return item is null ? null : new ExecutionLease(
            item.DispatchId, item.IntegrationExecutionId, item.LeaseToken!.Value, item.LeaseExpiresAt!.Value);
    }

    public async Task<bool> RenewAsync(Guid dispatchId, Guid token, int leaseSeconds, CancellationToken cancellationToken)
    {
        ValidateIdentity(dispatchId, token);
        ValidateDuration(leaseSeconds);
        var updated = await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [ExecutionDispatchInbox]
            SET [LeaseExpiresAt] = DATEADD(second, {leaseSeconds}, SYSDATETIMEOFFSET())
            WHERE [DispatchId] = {dispatchId} AND [LeaseToken] = {token}
              AND [CompletedAt] IS NULL AND [LeaseExpiresAt] > SYSDATETIMEOFFSET()
            """, cancellationToken);
        return updated == 1;
    }

    public async Task<bool> ReleaseAsync(Guid dispatchId, Guid token, DateTimeOffset? availableAt, CancellationToken cancellationToken)
    {
        ValidateIdentity(dispatchId, token);
        var updated = await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [ExecutionDispatchInbox]
            SET [LeaseToken] = NULL, [LeaseExpiresAt] = NULL, [AvailableAt] = {availableAt}
            WHERE [DispatchId] = {dispatchId} AND [LeaseToken] = {token}
              AND [CompletedAt] IS NULL AND [LeaseExpiresAt] > SYSDATETIMEOFFSET()
            """, cancellationToken);
        return updated == 1;
    }

    public async Task<bool> CompleteAsync(Guid dispatchId, Guid token, CancellationToken cancellationToken)
    {
        ValidateIdentity(dispatchId, token);
        var succeeded = (int)IntegrationExecutionStatus.Succeeded;
        var failed = (int)IntegrationExecutionStatus.Failed;
        var cancelled = (int)IntegrationExecutionStatus.Cancelled;
        var updated = await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE inbox
            SET [CompletedAt] = SYSDATETIMEOFFSET(), [LeaseToken] = NULL, [LeaseExpiresAt] = NULL
            FROM [ExecutionDispatchInbox] AS inbox
            INNER JOIN [IntegrationExecutions] AS execution ON execution.[Id] = inbox.[IntegrationExecutionId]
            WHERE inbox.[DispatchId] = {dispatchId} AND inbox.[LeaseToken] = {token}
              AND inbox.[CompletedAt] IS NULL AND inbox.[LeaseExpiresAt] > SYSDATETIMEOFFSET()
              AND execution.[Status] IN ({succeeded}, {failed}, {cancelled})
            """, cancellationToken);
        return updated == 1;
    }

    private static void ValidateIdentity(Guid dispatchId, Guid token)
    {
        if (dispatchId == Guid.Empty)
            throw new ArgumentException("DispatchId must be provided.", nameof(dispatchId));
        if (token == Guid.Empty)
            throw new ArgumentException("Lease token must be provided.", nameof(token));
    }

    private static void ValidateDuration(int leaseSeconds)
    {
        if (leaseSeconds is < 1 or > 86400)
            throw new ArgumentOutOfRangeException(nameof(leaseSeconds), "Lease duration must be between 1 and 86400 seconds.");
    }
}
