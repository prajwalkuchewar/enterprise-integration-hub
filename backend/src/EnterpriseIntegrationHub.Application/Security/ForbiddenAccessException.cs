namespace EnterpriseIntegrationHub.Application.Security;

/// <summary>
/// The caller is authenticated but is not allowed to perform the requested action.
/// Maps to HTTP 403. Use <see cref="UnauthorizedAccessException"/> for missing or invalid credentials (HTTP 401).
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
