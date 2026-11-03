using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using EnterpriseIntegrationHub.Application.Features.Executions.Worker;
using EnterpriseIntegrationHub.Application.Interfaces;
using EnterpriseIntegrationHub.Domain.Enums;

namespace EnterpriseIntegrationHub.Api.Delivery;

public sealed class HttpExecutionDestinationInvoker(
    IHttpClientFactory clients,
    DestinationNetworkPolicy policy,
    IConfiguration configuration) : IExecutionDestinationInvoker
{
    public const string ClientName = "ExecutionDestination";

    public async Task<DestinationInvocationResult> InvokeAsync(ExecutionAttemptWork work, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        DestinationInvocationResult Result(DestinationInvocationStatus status, int? code = null) =>
            new(status, code, timer.ElapsedMilliseconds);

        if (work.Protocol != ConnectorProtocol.REST || work.TimeoutSeconds is < 1 or > 86370 ||
            !policy.IsAllowedDestination(work.DestinationBaseUrl, out var uri))
            return Result(DestinationInvocationStatus.InvalidConfiguration);

        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Content = new StringContent(work.PayloadJson, Encoding.UTF8, "application/json");
        request.Headers.Add("Idempotency-Key", work.IdempotencyKey);
        if (!string.IsNullOrEmpty(work.CorrelationId))
        {
            if (!IsSafeValue(work.CorrelationId) ||
                !request.Headers.TryAddWithoutValidation("X-Correlation-ID", work.CorrelationId))
                return Result(DestinationInvocationStatus.InvalidConfiguration);
        }

        var credentials = configuration.GetSection($"DestinationCredentials:{work.DestinationConnectorId:D}");
        switch (work.AuthenticationType)
        {
            case ConnectorAuthenticationType.APIKey:
                var name = credentials["HeaderName"];
                var key = credentials["ApiKey"];
                if (string.IsNullOrWhiteSpace(name) || name.Length > 100 ||
                    !name.StartsWith("X-", StringComparison.OrdinalIgnoreCase) ||
                    name.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-') ||
                    name.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase) || !IsSafeValue(key) ||
                    !request.Headers.TryAddWithoutValidation(name, key))
                    return Result(DestinationInvocationStatus.InvalidConfiguration);
                break;
            case ConnectorAuthenticationType.Basic:
                var username = credentials["Username"];
                var password = credentials["Password"];
                if (!IsSafeValue(username) || username!.Contains(':') || !IsSafeValue(password))
                    return Result(DestinationInvocationStatus.InvalidConfiguration);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
                break;
            case ConnectorAuthenticationType.OAuth2:
            case ConnectorAuthenticationType.BearerToken:
                var token = credentials["AccessToken"];
                if (!IsSafeValue(token) || token!.Any(char.IsWhiteSpace))
                    return Result(DestinationInvocationStatus.InvalidConfiguration);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;
            default:
                return Result(DestinationInvocationStatus.InvalidConfiguration);
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(work.TimeoutSeconds));
        try
        {
            using var client = clients.CreateClient(ClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            var code = (int)response.StatusCode;
            var status = response.IsSuccessStatusCode ? DestinationInvocationStatus.Succeeded :
                code is 408 or 429 || code >= 500 ? DestinationInvocationStatus.TransientFailure :
                DestinationInvocationStatus.PermanentFailure;
            return Result(status, code);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Result(DestinationInvocationStatus.TransientFailure);
        }
        catch (HttpRequestException exception)
        {
            return Result(DestinationNetworkPolicy.IsPolicyFailure(exception)
                ? DestinationInvocationStatus.InvalidConfiguration : DestinationInvocationStatus.TransientFailure);
        }
    }

    private static bool IsSafeValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 8192 &&
        value.All(character => character is >= (char)0x20 and <= (char)0x7e);
}
