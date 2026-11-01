# Architecture Decision Records

---

## ADR-001

### Decision

Build the project as a Modular Monolith.

### Reason

The business domain is still being discovered.

A Modular Monolith allows rapid iteration while keeping clear module boundaries.

Future migration to microservices remains possible.

---

## ADR-002

### Decision

Implement Commands and Handlers manually.

### Reason

The project is intended as a learning platform.

Understanding the architecture is more important than introducing MediatR early.

Framework abstractions will be introduced only when they solve a demonstrated problem.

---

## ADR-003

### Decision

Repository interfaces belong in the Application layer.

### Reason

Application defines the persistence requirements.

Infrastructure implements those requirements.

This keeps business logic independent of Entity Framework.

---

## ADR-004

### Decision

Use business-first feature development.

### Reason

Every feature starts from a business capability rather than database tables or controllers.

Architecture should emerge naturally from business responsibilities.

---

## ADR-005

### Decision

Develop using complete vertical slices.

### Reason

Each feature should be functional from API to persistence before moving to the next capability.

This provides working software at every milestone.

---

# ADR-006

## Decision

Separate Command and Query handlers.

## Reason

Write operations and read operations evolve independently.

Separate handlers improve readability and allow future optimizations without affecting unrelated use cases.

Status

Accepted

---

# ADR-008

## Decision

Configuration records use SQL Server `rowversion` tokens for optimistic concurrency. An update based on a stale token fails and is returned as HTTP 409 so the caller can reload current configuration.

Each workflow starts at configuration revision 1. A successful update to a Draft workflow increments the revision once only when its effective configuration changes. An unchanged update does not increment it. Activation freezes that revision because active workflows cannot be edited.

When runtime execution is implemented, each execution will reference and snapshot the exact workflow revision it started with. Later revisions must not change the configuration used by an existing execution. Revision persistence and execution snapshots are part of Milestone 3.

## Reason

Rowversion prevents concurrent edits from silently overwriting one another. An explicit workflow revision gives API clients and future execution records a stable identifier for the configuration that was published.

Status

Accepted

---

# ADR-007

## Decision

Use Response Models instead of returning Domain Entities from the API.

## Reason

The API should expose contracts tailored to each use case rather than leaking internal domain models.

This allows the Domain Model to evolve independently of API consumers.

Status

Accepted

---

# ADR-009 — Runtime execution history

## Decision

An `IntegrationExecution` is the durable record of one accepted source event processed against one immutable `WorkflowRevision`. It owns ordered `ExecutionStep` records; each step owns append-only invocation attempts. Execution history records sanitized metadata and outcomes, not secrets or unredacted response bodies.

The first runtime is sequential and at-least-once. Automatic retries append attempts to the failed step and do not repeat completed steps. A step failure stops later steps without compensating earlier external side effects. Operator replay creates a new execution pinned to the original revision and resumes at the failed step by default. Execution creation, step snapshots, and dispatch intent are committed together; external calls occur outside the database transaction.

See [Runtime Execution Domain Decisions](execution-domain-decisions.md) for ownership boundaries, states, transition table, idempotency, failure classification, and scope.

## Reason

Durable immutable history makes event handling auditable, makes retry behavior explicit, and prevents workflow edits from changing the plan of an execution already in progress. At-least-once delivery reflects the limits of coordinating a database with external systems.

Status

Accepted in PR #65; execution persistence is implemented on the dependent #50 branch. Worker execution remains future work.

---
