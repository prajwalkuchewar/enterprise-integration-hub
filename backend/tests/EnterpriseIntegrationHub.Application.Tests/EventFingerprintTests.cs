using System;
using System.Text.Json;
using EnterpriseIntegrationHub.Application.Features.Events.Ingest;
using FluentAssertions;
using Xunit;

namespace EnterpriseIntegrationHub.Application.Tests;

public class EventFingerprintTests
{
    private static readonly DateTimeOffset OccurredAt = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    [Fact]
    public void Compute_IgnoresPropertyOrderAndWhitespace()
    {
        var first = EventFingerprint.Compute("order.created", OccurredAt, 1, Json("{\"a\":1,\"b\":{\"x\":\"y\",\"z\":2}}"));
        var second = EventFingerprint.Compute("order.created", OccurredAt, 1, Json("{ \"b\": { \"z\": 2, \"x\": \"y\" }, \"a\": 1 }"));

        second.Should().Be(first);
    }

    [Fact]
    public void Compute_ReturnsLowercaseSha256Hex()
    {
        var fingerprint = EventFingerprint.Compute("order.created", OccurredAt, 1, Json("{}"));

        fingerprint.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Compute_ChangesWhenPayloadValueChanges()
    {
        var original = EventFingerprint.Compute("order.created", OccurredAt, 1, Json("{\"amount\":10}"));
        var changed = EventFingerprint.Compute("order.created", OccurredAt, 1, Json("{\"amount\":11}"));

        changed.Should().NotBe(original);
    }

    [Fact]
    public void Compute_ChangesWhenEventTypeOrSchemaVersionChanges()
    {
        var payload = Json("{\"amount\":10}");
        var baseline = EventFingerprint.Compute("order.created", OccurredAt, 1, payload);

        EventFingerprint.Compute("order.updated", OccurredAt, 1, payload).Should().NotBe(baseline);
        EventFingerprint.Compute("order.created", OccurredAt, 2, payload).Should().NotBe(baseline);
    }

    [Fact]
    public void Compute_ChangesWhenArrayOrderChanges()
    {
        var first = EventFingerprint.Compute("order.created", OccurredAt, 1, Json("{\"items\":[1,2]}"));
        var second = EventFingerprint.Compute("order.created", OccurredAt, 1, Json("{\"items\":[2,1]}"));

        second.Should().NotBe(first);
    }
}
