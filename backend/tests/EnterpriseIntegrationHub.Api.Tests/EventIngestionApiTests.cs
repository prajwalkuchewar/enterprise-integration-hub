using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Api.Tests;

public sealed class EventIngestionApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Ingest_WhenKeyBelongsToDifferentSourceSystem_ReturnsForbidden()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();
        var connectorId = await CreateConnectorForSourceAsync(client, "Billing");
        var ingestionKey = await IssueIngestionKeyAsync(client, connectorId);

        using var response = await PostEventAsync(client, ingestionKey, sourceSystem: "Orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ingest_WhenKeyIsUnknown_ReturnsUnauthorized()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostEventAsync(client, "eih_not_a_real_key", sourceSystem: "Orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ingest_WhenKeyHeaderIsMissing_ReturnsUnauthorized()
    {
        using var factory = new EnterpriseIntegrationHubApiFactory();
        using var client = factory.CreateClient();

        using var response = await PostEventAsync(client, ingestionKey: null, sourceSystem: "Orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<Guid> CreateConnectorForSourceAsync(HttpClient client, string externalSystemName)
    {
        using var externalSystem = await PostJsonAsync(client, "/api/external-systems",
            new { name = externalSystemName, description = "Ingestion API test system", environment = 1 });
        externalSystem.StatusCode.Should().Be(HttpStatusCode.Created);
        using var externalSystemBody = JsonDocument.Parse(await externalSystem.Content.ReadAsStringAsync());
        var externalSystemId = externalSystemBody.RootElement.GetProperty("id").GetGuid();

        using var connector = await PostJsonAsync(client, "/api/connectors", new
        {
            name = $"{externalSystemName} connector",
            description = "Ingestion API test connector",
            externalSystemId,
            baseUrl = "https://connector.example.test",
            protocol = 1,
            authenticationType = 1,
            timeoutSeconds = 30
        });
        connector.StatusCode.Should().Be(HttpStatusCode.Created);
        using var connectorBody = JsonDocument.Parse(await connector.Content.ReadAsStringAsync());
        return connectorBody.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> IssueIngestionKeyAsync(HttpClient client, Guid connectorId)
    {
        using var response = await client.PostAsync($"/api/connectors/{connectorId}/ingestion-key", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("ingestionKey").GetString()!;
    }

    private static Task<HttpResponseMessage> PostEventAsync(HttpClient client, string? ingestionKey, string sourceSystem)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/events")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                eventId = "evt-api-1",
                eventType = "order.created",
                sourceSystem,
                occurredAt = DateTimeOffset.UtcNow,
                schemaVersion = 1,
                payload = new { amount = 10 }
            }, JsonOptions), Encoding.UTF8, "application/json")
        };
        if (ingestionKey is not null)
            request.Headers.Add("X-Ingestion-Key", ingestionKey);

        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object payload) =>
        client.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"));
}
