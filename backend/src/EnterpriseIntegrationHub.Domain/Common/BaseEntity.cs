namespace EnterpriseIntegrationHub.Domain.Common;

public abstract class BaseEntity
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset? UpdatedAt { get; private set; }
}
