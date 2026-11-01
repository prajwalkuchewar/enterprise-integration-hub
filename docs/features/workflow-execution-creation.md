# Workflow Execution Creation

**Status:** Implemented on the dependent P1 #50 branch. It does not expose a public ingestion endpoint or invoke destination connectors.

## Creation contract

The caller supplies a normalized event envelope containing source connector ID, source event ID, trigger event, occurrence time, optional correlation ID, and a SHA-256 fingerprint of the canonical event type, occurrence time, and payload. The raw payload is not persisted. The handler validates the envelope, deduplicates by `(SourceConnectorId, SourceEventId)`, resolves the active workflow revision, then persists:

- the immutable workflow revision snapshot (created when the workflow is activated);
- an `IntegrationExecution` in `Queued` state;
- one ordered `ExecutionStep` snapshot per workflow revision step, initially `Pending`;
- one dispatch outbox record.

The execution, step snapshots, and dispatch intent are written in one database transaction. No outbound request is made while creating the execution. The worker and dispatch publisher are later work.

## Idempotency

- A repeat event identity with the same event fingerprint returns the existing execution ID and status without adding another outbox record.
- Reuse of the same source event ID with a different fingerprint returns a conflict.
- The SQL unique index is the final guard for concurrent duplicate deliveries; the repository reloads the winner after a unique-key race.

## Snapshot and data boundary

Each execution references its immutable workflow revision and stores the ordered revision-step IDs, destination connector IDs, and non-secret endpoint/protocol/authentication-type/timeout values. Secret values and raw event payloads are not stored. The event fingerprint and correlation metadata support deduplication and tracing.

Existing active workflows are backfilled into immutable revision records by the migration. New revision records are created atomically with workflow activation.

## Not included

This capability does not receive events over HTTP, publish/consume the outbox, invoke destinations, retry work, or expose execution history through the API. Those belong to later runtime vertical slices.
