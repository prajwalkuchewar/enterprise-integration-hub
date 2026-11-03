# Event Ingestion

Source systems submit events to the hub through an authenticated webhook. Each accepted event is recorded as a durable integration execution, together with an outbox row that will drive downstream dispatch.

## 1. Issue an ingestion key

Each connector has at most one active ingestion key. The plaintext key is returned only once.

```http
POST /api/connectors/{connectorId}/ingestion-key
```

Response `201 Created`:

```json
{ "connectorId": "…", "ingestionKey": "eih_…" }
```

Issuing a new key invalidates the previous one. Store the key in the source system's secret store.

## 2. Submit an event

```http
POST /api/events
X-Ingestion-Key: eih_…
Content-Type: application/json
```

```json
{
  "eventId": "emp-1001-update-42",
  "eventType": "employee.updated",
  "sourceSystem": "Payroll System",
  "occurredAt": "2026-01-15T10:30:00Z",
  "correlationId": "optional-trace-id",
  "schemaVersion": 1,
  "payload": { "employeeId": "1001", "salary": 85000 }
}
```

| Status | Meaning |
| --- | --- |
| `202 Accepted` | New event recorded. Body includes `executionId`, `status`, `alreadyAccepted: false`. |
| `200 OK` | The same event (same content) was already accepted. No new execution is created. |
| `400 Bad Request` | Invalid envelope, for example a missing `eventId` or a payload that is not a JSON object. |
| `401 Unauthorized` | Missing or unknown ingestion key. |
| `403 Forbidden` | Authenticated key is valid, but `sourceSystem` does not match the external system that owns the key. |
| `404 Not Found` | No active workflow is triggered by this `eventType` for the connector. |
| `409 Conflict` | The connector is not Active, or the `eventId` was reused with different content. |

## Duplicate handling

- `eventId` is unique per source connector.
- Re-sending identical content returns `200` and does not create a second execution.
- Re-sending the same `eventId` with different content returns `409`.
- Whitespace and property order in the payload do not affect duplicate detection. Array order and number formatting do.

## Known gaps

- Conflicting duplicates are rejected but not yet recorded as a diagnostic entry.
- Downstream dispatch (the outbox worker) is not yet implemented, so accepted executions stay `Queued`.
