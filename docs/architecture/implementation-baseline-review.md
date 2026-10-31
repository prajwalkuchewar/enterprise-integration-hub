# Execution-Readiness Review

**Review date:** 2026-10-31
**Purpose:** identify work that must be completed before EIH performs real external integration side effects.

## Current assessment

The configuration and governance foundation is implemented: full-stack system, connector, and workflow management; domain lifecycle behavior; persistence; tests; and backend CI.

The system is **not ready to execute integrations**. This is an expected product stage, not a failure. Runtime work introduces external side effects, credentials, asynchronous delivery, and recovery responsibilities that require explicit design and verification.

## Findings

| ID | Finding | Required action |
| --- | --- | --- |
| ER-01 | Connector/external-system deactivation does not yet resolve active workflow dependencies. | Choose and implement a policy; block dependent connector deactivation is the recommended initial policy. |
| ER-02 | Essential connector and external-system invariants are now enforced by the domain; request-boundary validation remains uneven. | Standardize remaining request validation and retain the domain as final authority. |
| ER-03 | HTTP error mapping is controller-local. | Add centralized exception handling and stable ProblemDetails responses. |
| ER-04 | Tests do not yet include API-host or SQL Server relational coverage. | Add WebApplicationFactory/API-host tests and SQL Server container integration tests. |
| ER-05 | Mutable configuration has no optimistic concurrency or published revision model. | Add concurrency control and immutable workflow revisions before runtime reads configuration. |
| ER-06 | Backend CI does not compile the Angular frontend. | Add a frontend build job/path coverage. |
| ER-07 | Security and operations architecture is not yet approved. | Define auth/RBAC, secret storage, audit, logging, metrics, tracing, health, and environment policy. |
| ER-08 | Runtime reliability contract is undefined. | Define delivery semantics, idempotency, timeouts, retry/backoff, worker recovery, dead-letter, and replay. |

## Gate

Do not begin a connector runtime or external side-effecting worker until all of the following are complete:

- [ ] ER-01 through ER-06 are implemented and covered by tests.
- [ ] Security and operational ADRs are approved.
- [ ] The normalized event and execution-state models are approved.
- [ ] Delivery, idempotency, retry, and replay semantics are documented.
- [ ] A threat model and one failure-mode walkthrough are complete.

## What may proceed now

Documentation, frontend CI, configuration hardening, API-host tests, SQL Server tests, concurrency/versioning design, and ADR work may proceed immediately. The ordered work breakdown is in the [Implementation Plan](../implementation-plan.md).
