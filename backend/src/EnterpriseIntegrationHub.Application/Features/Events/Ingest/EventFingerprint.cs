using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace EnterpriseIntegrationHub.Application.Features.Events.Ingest;

/// <summary>
/// Computes a content fingerprint that ignores JSON whitespace and object property order,
/// so a retried delivery with the same meaning matches while changed content does not.
/// </summary>
public static class EventFingerprint
{
    public static string Compute(string eventType, DateTimeOffset occurredAt, int schemaVersion, JsonElement payload)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("eventType", eventType.Trim());
            writer.WriteString("occurredAt", occurredAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            writer.WriteNumber("schemaVersion", schemaVersion);
            writer.WritePropertyName("payload");
            WriteCanonical(writer, payload);
            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteCanonical(writer, item);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
