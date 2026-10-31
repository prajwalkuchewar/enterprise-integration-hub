# Workflow Resolution

**Status:** Implemented on P1 #51/#50 branches as an internal application capability. It does not expose an ingestion endpoint or create executions.

## Contract

The caller supplies the source connector ID and trigger-event identifier from a normalized event. The resolver:

1. Considers active workflows for that source connector only.
2. Matches the trigger-event identifier after trimming whitespace, case-insensitively.
3. Returns a no-match result when no active workflow matches.
4. Returns a conflict when more than one active workflow matches; it never selects an arbitrary candidate.
5. Requires the matched workflow's source and destination connectors to still be Active.
6. Returns the persisted immutable workflow revision, including ordered destinations and their non-secret connector configuration needed by execution.

The result is read-only. It is passed to the execution-creation capability (#50), which persists the exact workflow revision and step snapshots together with its dispatch intent. Resolution itself performs no destination side effects and creates no execution record.

## Failure behavior

- Invalid source connector ID or blank event type: `ArgumentException` (HTTP 400 through the existing exception contract).
- No active matching workflow: `KeyNotFoundException` (HTTP 404).
- Multiple active matches, or a matched workflow with an inactive source/destination connector: `InvalidOperationException` (HTTP 409).

## Boundary

This handler is an application capability for the future ingestion/execution flow. There is no public API route until an authenticated event-ingestion boundary is designed.
