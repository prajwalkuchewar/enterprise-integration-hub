namespace EnterpriseIntegrationHub.Application.Features.Executions.Worker;

public sealed class ExecutionWorkerOptions
{
    public const string SectionName = "ExecutionWorker";
    public bool Enabled { get; set; }
    public int PollIntervalSeconds { get; set; } = 2;
    public int LeaseSeconds { get; set; } = 60;
    public int MaxAttempts { get; set; } = 5;
    public int BaseRetryDelaySeconds { get; set; } = 2;
    public int MaxRetryDelaySeconds { get; set; } = 300;

    public void Validate()
    {
        if (PollIntervalSeconds is < 1 or > 3600 || LeaseSeconds is < 1 or > 86400 ||
            MaxAttempts is < 1 or > 100 || BaseRetryDelaySeconds is < 1 or > 2592000 ||
            MaxRetryDelaySeconds < BaseRetryDelaySeconds || MaxRetryDelaySeconds > 2592000)
            throw new InvalidOperationException("Execution worker options are invalid.");
    }
}
