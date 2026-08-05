# Implementation Baseline and Architectural Review

**Review date:** 2026-08-05  
**Baseline:** `846cd68` (`main`), plus the current uncommitted `UpdateConnectorHandler` registration in `Startup`  
**Scope:** Implemented configuration capabilities through Sprint 3; no runtime integration execution capability is included in this review.

This document is the technical source of truth for the current implemented foundation. Update it after each major milestone, alongside the feature documentation and relevant ADRs. It distinguishes verified implementation from planned work so future decisions are based on the system that exists, not the system intended.

## Executive Assessment

The project has a sound modular-monolith foundation. Its dependency direction is clean, its main aggregates express meaningful workflow and connector state transitions, and the delivered capabilities are organized as vertical slices. The code is suitable to continue configuration work after one runtime defect is fixed.

It is **not yet ready for integration execution**. Execution will make connector configuration security-sensitive, introduce retries and asynchronous failure modes, and require dependable observability. The priority work below should be completed before Sprint 6 begins.

| Area                | Assessment                                            | Evidence                                                                                                                              |
| ------------------- | ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Layering            | Healthy                                               | API depends on Application and Infrastructure; Application depends on Domain; Infrastructure implements Application repository ports. |
| Business model      | Promising, with a boundary gap                        | `Workflow` protects its invariants, while `Connector` and `ExternalSystem` constructors accept invalid state directly.                |
| Repositories        | Appropriate for the current aggregates                | One repository per aggregate root; SQL uniqueness constraints back application existence checks.                                      |
| Handlers            | Mostly thin                                           | Handlers orchestrate repositories and aggregate methods; validation and repeated connector checks are beginning to accumulate.        |
| HTTP API            | Resource-oriented and usable                          | Collections and item routes are RESTful; activation is an explicit state-transition subresource action.                               |
| Persistence         | Adequate for configuration writes                     | EF Core persists each aggregate with one `SaveChangesAsync`; relational EF Core saves are transactional by default.                   |
| Test confidence     | Good unit coverage, incomplete integration confidence | 78 passing unit/in-memory tests; no HTTP-host or SQL Server relational integration suite.                                             |
| Execution readiness | Blocked                                               | Missing runtime DI registration plus security, observability, resilience, and execution architecture work.                            |

## Implemented System

### Architectural Shape

```text
HTTP API
  -> Application commands, queries, and handlers
    -> Domain aggregates and rules
    <- Infrastructure EF Core repositories
      -> SQL Server
```

The implementation follows the intended API -> Application -> Domain -> Infrastructure model documented in [system-overview.md](system-overview.md). Repository interfaces are owned by Application and implemented by Infrastructure, preserving the dependency rule described in ADR-003. API response models prevent domain entities from being exposed directly.

### Delivered Capabilities

| Module           | Delivered use cases                            | Current business behavior                                                                                                                                                    |
| ---------------- | ---------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| External systems | Create, browse, view details                   | External systems are currently registered as active and are not yet editable through the API.                                                                                |
| Connectors       | Create, browse, view details, update, activate | A connector starts as Draft. Updating communication settings on an Active connector intentionally returns it to Draft; basic-information-only changes retain its status.     |
| Workflows        | Create, browse, view details, update, activate | A workflow is a source connector plus one or more ordered destinations. It can activate only when all referenced connectors are Active; only Draft workflows may be updated. |

`Workflow` is the strongest aggregate boundary: it owns and validates its ordered steps. The persistence model reinforces the design with a composite workflow-step key, cascading ownership deletion, foreign keys, and unique workflow/connector-name indexes.

### Verified Baseline

- `dotnet test EnterpriseIntegrationHub.sln --no-restore --verbosity minimal` completed successfully on 2026-08-05.
- Result: 78 passed, 0 failed, 0 skipped.
- The test suite covers domain behavior, handler paths, and repositories using EF Core's InMemory provider.
- The implementation has no API-host integration tests and no tests against SQL Server, so this result does not validate controller wiring, dependency injection, HTTP error contracts, migrations, or relational constraints.

## Architectural Review Findings

### Must Fix Before the Next Release

| ID     | Finding                                                                                                                              | Why it matters                                                                                                                                                                                                | Required action                                                                                                                                                                                                                                                          |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| AR-001 | `UpdateConnectorHandler` was not registered in the reviewed commit; the current worktree now registers it as scoped.                 | The immediate runtime DI failure is resolved locally, but it is not yet part of the recorded commit baseline and has no API-host regression test.                                                             | Commit the registration and add an API-host smoke test that resolves every controller endpoint.                                                                                                                                                                          |
| AR-002 | Connector and external-system constructors permit invalid domain state; their invariants live principally in application validators. | Any future caller, import, message consumer, or test can create invalid domain objects without going through a handler. This weakens business-first design exactly where execution will add new entry points. | Move essential invariants into aggregate construction/methods: required identifiers and names, positive timeout, valid HTTP/HTTPS endpoint, valid enum values, and valid initial status. Keep API validation for client feedback, but do not rely on it for correctness. |

### Refactor Before Integration Execution

| ID     | Finding                                                                                                                                                                                 | Current impact                                                                                                                         | Recommended direction                                                                                                                                                                                                                                            |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| AR-003 | Exception-to-HTTP mapping is repeated in all controllers and returns anonymous `{ message }` objects.                                                                                   | New endpoints can drift in status mapping and clients have no stable problem contract.                                                 | Add centralized exception handling that emits RFC 7807 `ProblemDetails`; define application-specific exception types for validation, missing resources, and business conflicts. Keep controllers as request/response mapping only.                               |
| AR-004 | Connector create and update validators duplicate URL, name, and timeout checks, and handlers instantiate validators directly.                                                           | Validation changes must be made in multiple places; handlers own construction details.                                                 | Extract shared connector input validation and inject it or make it a domain-owned factory/value object. Do not add a broad framework abstraction until it replaces this demonstrated duplication.                                                                |
| AR-005 | Repeated connector-status lookup logic exists in workflow create and activation handlers.                                                                                               | The rule is consistent today but will drift as connector lifecycle rules expand.                                                       | Introduce a small application service or policy for resolving an active connector, provided it remains focused on this one business rule.                                                                                                                        |
| AR-006 | Tests exercise repositories through the EF InMemory provider only.                                                                                                                      | It does not validate SQL Server foreign keys, unique indexes, transactions, collation, or migrations.                                  | Add a SQL Server container/integration test path for repository constraints and an ASP.NET Core `WebApplicationFactory` suite for the primary API flows.                                                                                                         |
| AR-007 | There is no concurrency policy on mutable connector and workflow aggregates.                                                                                                            | Concurrent updates can overwrite each other without detection, which becomes hazardous once execution reads active configuration.      | Add an optimistic concurrency token and map conflicts to `409 Conflict`; define whether in-flight executions use a configuration version or immutable published revision.                                                                                        |
| AR-008 | Execution security and operations capabilities are not designed yet. CORS allows every origin, no authorization policy is configured, and connection settings are development-oriented. | Running integrations would expose endpoints and external credentials without an authorization, secret, audit, or operational boundary. | Before execution, define authentication/authorization, secret storage and encryption, tenant/access boundaries, audit events, structured logging, metrics, tracing, health checks, retries, idempotency, dead-letter handling, and a connection-security policy. |

### Improve When the Related Surface Changes

| ID     | Finding                                                                                                                              | Recommendation                                                                                                                                       |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| AR-009 | API routes have no versioning policy.                                                                                                | Adopt and document a versioning strategy before external clients depend on the API; URL versioning is a straightforward fit for the existing routes. |
| AR-010 | The roadmap lists "Workflow Steps" as Sprint 4 even though workflow steps are already implemented as part of the Workflow aggregate. | Rename or redefine Sprint 4 so planned work is unambiguous, such as step-level editing, validation rules, or transformation bindings.                |
| AR-011 | The update validator class is stored in `Features/Connectors/Update/CreateConnectorCommandValidator.cs`.                             | Rename the file to `UpdateConnectorCommandValidator.cs` to preserve feature-slice discoverability.                                                   |
| AR-012 | Request-level validation is uneven for identifiers and enum values.                                                                  | Standardize request validation, including rejecting empty GUIDs and undefined enum values. The domain remains the final authority.                   |

## Deliberate Non-Findings

These items were reviewed and are not current defects:

- **Detached repository reads:** repositories intentionally use `AsNoTracking()` and explicitly attach a modified aggregate with `DbSet.Update()` before saving. This is a valid disconnected-update pattern.
- **Workflow aggregate persistence:** workflow and its steps are added in a single EF Core `SaveChangesAsync` call. With the configured relational provider, EF Core wraps a single save operation in a transaction by default.
- **Connector re-drafting on communication change:** this is tested behavior, not an accidental state change. It should be documented as a product rule and retained unless the business changes it.
- **Activation endpoints:** `POST /{resource}/{id}/activate` is an appropriate explicit action for a state transition; forcing it into a generic field update would make the lifecycle less clear.

## Pre-Execution Gate

Do not begin integration execution until all of the following are complete:

- [ ] AR-001 is committed and covered by an API-host test; AR-002 is resolved and covered by tests.
- [ ] Central error handling and stable API error contracts exist.
- [ ] A SQL Server integration test suite and an HTTP-host test suite validate the critical configuration flows.
- [ ] Optimistic concurrency and configuration-version semantics are decided and implemented.
- [ ] Authentication, authorization, secret management, audit logging, metrics, tracing, health checks, and failure-handling strategy are approved in ADRs.
- [ ] The execution model specifies delivery guarantees, idempotency, retry/backoff, timeout/circuit-breaker behavior, and dead-letter/replay ownership.
- [ ] The Sprint 4 roadmap is reconciled with the implemented workflow-step aggregate.

## Review Cadence

At each major milestone, update this document with:

1. The commit or release baseline and test result.
2. Changes to implemented capabilities, aggregate boundaries, API contracts, and persistence design.
3. New findings, resolved findings, and consciously accepted risks.
4. The next milestone gate, especially before any change that introduces external side effects, credentials, asynchronous processing, or customer-facing contracts.

This keeps the architecture review a decision record for the system as built, rather than a retrospective code-style checklist.
