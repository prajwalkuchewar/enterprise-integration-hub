# Current Implementation Dossier

**Baseline:** reviewed 2026-10-31. This replaces the earlier August baseline, which predates the Angular workspace and later lifecycle work.

## Product boundary

EIH is a .NET 9 and Angular 20 full-stack modular monolith for integration configuration and governance. It is not yet an ingestion, transformation, or execution platform.

| Area | Current implementation |
| --- | --- |
| External systems | Create, browse, details, update, activate, deactivate. |
| Connectors | Create, browse, details, update, activate, deactivate. |
| Workflows | Create, browse, details, update while Draft, activate. |
| Workflow steps | Owned by Workflow; created, updated, and retrieved in order. |
| Frontend | Angular 20 API-backed dashboard and management flows. |
| Persistence | EF Core SQL Server DbContext, configurations, migrations, repositories. |
| API | Three REST controllers and Development Swagger. |
| Tests | Domain, application, and infrastructure test projects. |
| Automation | Backend formatting, release build, test, and coverage-artifact CI. |

## Architecture

Angular UI → ASP.NET Core API → Application handlers → Domain model → Infrastructure repositories → SQL Server.

Manual command/query handlers were chosen over MediatR. Repository interfaces live in Application and Infrastructure implements them. SharedKernel is currently a placeholder project.

## Important behavior

- Workflows own their steps and enforce structural validity.
- Connector and external-system constructors now enforce essential names, descriptions, IDs, URL, enum, and timeout invariants.
- Workflows are editable and activatable only while Draft.
- Workflow activation confirms all referenced connectors are Active.
- Communication-critical connector changes redraft active connectors.
- External-system and connector lifecycle endpoints are implemented.

## Execution blockers

1. Define dependency-safe connector/external-system deactivation.
2. Build frontend in CI.
3. Establish a global ProblemDetails error contract.
4. Add API-host and SQL Server relational tests.
5. Add optimistic concurrency and configuration versioning.
6. Design security, secrets, audit, ingestion, reliability, and runtime execution.

See the [implementation plan](../implementation-plan.md).
