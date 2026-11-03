namespace EnterpriseIntegrationHub.Application.Features.Executions.DispatchOutbox;

public sealed class DispatchOutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; set; }
    public int BatchSize { get; set; } = 25;
    public int PollIntervalSeconds { get; set; } = 2;
    public int LeaseSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 8;
    public int BaseRetryDelaySeconds { get; set; } = 2;
    public int MaxRetryDelaySeconds { get; set; } = 300;
}
