using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using EnterpriseIntegrationHub.Application.Features.Executions.Worker;
using EnterpriseIntegrationHub.Application.Interfaces;

namespace EnterpriseIntegrationHub.Api.Delivery;

internal sealed class OAuth2TokenProvider(IHttpClientFactory clients, DestinationNetworkPolicy policy)
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<Guid, CachedToken> _tokens = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    public async Task<OAuth2TokenResult> GetAccessTokenAsync(
        Guid connectorId, IConfigurationSection credentials, CancellationToken cancellationToken)
    {
        var tokenUrl = credentials["TokenUrl"];
        var clientId = credentials["ClientId"];
        var clientSecret = credentials["ClientSecret"];
        var scope = credentials["Scope"];
        if (!policy.IsAllowedDestination(tokenUrl ?? string.Empty, out var uri) ||
            !HttpExecutionDestinationInvoker.IsSafeValue(clientId) ||
            !HttpExecutionDestinationInvoker.IsSafeValue(clientSecret) ||
            (scope is not null && !HttpExecutionDestinationInvoker.IsSafeValue(scope)))
            return OAuth2TokenResult.Failed(DestinationInvocationStatus.InvalidConfiguration);

        var fingerprint = new CredentialFingerprint(tokenUrl!, clientId!, scope);
        if (TryGetFresh(connectorId, fingerprint, out var cached))
            return OAuth2TokenResult.Succeeded(cached);

        var gate = _gates.GetOrAdd(connectorId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (TryGetFresh(connectorId, fingerprint, out cached))
                return OAuth2TokenResult.Succeeded(cached);
            return await FetchAsync(connectorId, uri!, fingerprint, clientId!, clientSecret!, scope, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetFresh(Guid connectorId, CredentialFingerprint fingerprint, out string token)
    {
        token = string.Empty;
        if (!_tokens.TryGetValue(connectorId, out var cached) || cached.Fingerprint != fingerprint ||
            cached.ExpiresAt - RefreshSkew <= DateTimeOffset.UtcNow)
            return false;
        token = cached.AccessToken;
        return true;
    }

    private async Task<OAuth2TokenResult> FetchAsync(
        Guid connectorId, Uri tokenUri, CredentialFingerprint fingerprint,
        string clientId, string clientSecret, string? scope, CancellationToken cancellationToken)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret)
        };
        if (!string.IsNullOrWhiteSpace(scope))
            fields.Add(new("scope", scope));

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUri)
        {
            Content = new FormUrlEncodedContent(fields)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var client = clients.CreateClient(HttpExecutionDestinationInvoker.ClientName);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var code = (int)response.StatusCode;
            return OAuth2TokenResult.Failed(code is 408 or 429 || code >= 500
                ? DestinationInvocationStatus.TransientFailure
                : DestinationInvocationStatus.InvalidConfiguration);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (!root.TryGetProperty("access_token", out var tokenElement) || tokenElement.ValueKind != JsonValueKind.String)
            return OAuth2TokenResult.Failed(DestinationInvocationStatus.InvalidConfiguration);

        var token = tokenElement.GetString();
        if (token is null || !HttpExecutionDestinationInvoker.IsSafeValue(token) || token.Any(char.IsWhiteSpace))
            return OAuth2TokenResult.Failed(DestinationInvocationStatus.InvalidConfiguration);

        var lifetime = root.TryGetProperty("expires_in", out var expiresIn) &&
            expiresIn.ValueKind == JsonValueKind.Number && expiresIn.TryGetInt32(out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : DefaultLifetime;
        _tokens[connectorId] = new CachedToken(token, DateTimeOffset.UtcNow + lifetime, fingerprint);
        return OAuth2TokenResult.Succeeded(token);
    }

    private sealed record CredentialFingerprint(string TokenUrl, string ClientId, string? Scope);

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAt, CredentialFingerprint Fingerprint);
}

internal sealed record OAuth2TokenResult(string? AccessToken, DestinationInvocationStatus? Failure)
{
    public static OAuth2TokenResult Succeeded(string token) => new(token, null);

    public static OAuth2TokenResult Failed(DestinationInvocationStatus status) => new(null, status);
}
