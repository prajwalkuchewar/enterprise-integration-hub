# Implementation Plan — From Configuration to Reliable Execution

**Status:** Milestone 1 complete; later runtime milestones remain planned. This plan is not a claim that the runtime exists.

## Milestone 0 — Baseline and quality

- [x] Reconcile implementation documentation with current repository state.
- [ ] Add current screenshots/GIF and a demo script to the README.
- [ ] Update CI to build Angular for frontend changes.
- [ ] Record release baseline and test result at each roadmap milestone.

**Exit:** docs reflect main and CI verifies backend and frontend.

## Milestone 1 — Configuration hardening

1. [x] Move essential connector/external-system invariants into domain behavior; reject empty IDs and invalid enum values at the boundary.
2. [x] Block connector deactivation when an active workflow references it; return a clear conflict.
3. [x] Block external-system deactivation when active connectors or workflows depend on it.
4. [x] Add centralized exception handling with a stable ProblemDetails contract.
5. [x] Add API-host lifecycle tests and SQL Server relational integration tests.
6. [x] Add SQL Server rowversion optimistic concurrency for configuration records and map stale updates to 409.
7. [x] Define workflow revision semantics: revisions begin at 1, increment only for changed Draft configuration, and the activated revision is immutable. Runtime executions will persist the exact revision they use.

The SQL Server tests use Testcontainers by default. Set `EIH_SQLSERVER_TEST_CONNECTION` to use an existing SQL Server instance instead; Docker is required when no connection string is configured.

**Exit:** met. Configuration writes have relational constraints, lifecycle rules, request validation, conflict handling, integration coverage, and persisted concurrency tokens. Workflow API responses expose the current configuration revision. Execution persistence and revision snapshots remain part of Milestone 3.

## Milestone 2 — Runtime and security design

Write ADRs for:

- Normalized event envelope: ID, type, source, occurred time, correlation ID, payload metadata, schema version.
- Ingestion boundary and first adapter (recommend authenticated webhook).
- At-least-once delivery and idempotency ownership.
- Execution states, sequential behavior, timeout, retry classification, cancellation, and compensation policy.
- Authentication/roles, secret boundary, audit events, CORS, and environment separation.
- Logging, metrics, tracing, health checks, retention, and alerts.
- Durable database queue versus Redpanda/broker decision.

**Exit:** ADRs and a threat model are approved; one end-to-end scenario includes failure semantics.

## Milestone 3 — Execution foundation

Implement WorkflowRevision, IntegrationExecution, and ExecutionStep models; resolver; transactional execution/step creation; execution read APIs and UI; and tests for state transitions and revision snapshotting.

**Exit:** a normalized event creates a durable, queryable execution linked to its workflow revision, with no external side effect yet.

## Milestone 4 — One HTTP execution slice

Implement typed HTTP invocation, timeout/cancellation, secure secret references, redacted metadata, one worker, persistent success/failure result, idempotency, and a stub-service integration test.

**Exit:** one authenticated event invokes one HTTP destination and records accurate history.

## Milestone 5 — Resilience and recovery

Implement bounded exponential retries, durable scheduling, dead-letter handling, authorized replay with audit trail, restart recovery, worker lease/concurrency rules, failure injection, and load tests.

**Exit:** transient failures recover safely; terminal failures are visible and controllably replayable.

## Milestone 6 — Transformations and multi-step execution

Add versioned field mappings, validation, redacted sample data, ordered multi-step execution, persisted step results, and stop-on-failure semantics. Parallel execution and compensation remain deferred until justified.

**Exit:** an ordered workflow runs end-to-end with transformation results and step history.

## Milestone 7 — Operations and production readiness

Add dashboard/search, OpenTelemetry, structured logs, metrics, health/alerts, auth/RBAC, secret rotation, audit history, deployment CI/CD, migration/backups, security review, E2E/performance/disaster-recovery tests, and runbooks.

**Exit:** a documented deployment is secure, observable, recoverable, and operable by someone other than the developer.

## First ticket

**Block connector deactivation when an active workflow references the connector.** It is a small, real business policy that improves current correctness and exercises dependency checking before runtime complexity.
