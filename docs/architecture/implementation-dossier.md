# Implementation Dossier

**Purpose:** Reviewer-facing technical inventory of the system as implemented.  
**Baseline:** `846cd68` on `main`, reviewed 2026-08-05, plus the current uncommitted `UpdateConnectorHandler` registration.  
**Companion:** [implementation-baseline-review.md](implementation-baseline-review.md) contains findings, architectural assessment, and the pre-execution gate. This dossier records implementation facts.

## Scope at This Baseline

Enterprise Integration Hub is currently a .NET 9 modular monolith for **integration configuration**, not integration execution. It registers external systems, defines connectors, and defines/activates workflows that route from one connector to ordered destination connectors.

Implemented:

- External-system registration, browse, and detail retrieval.
- Connector creation, browse, detail retrieval, full replacement update, and activation.
- Workflow creation, browse, detail retrieval, full replacement update, and activation.
- SQL Server EF Core persistence, migrations, local infrastructure containers, CI, unit tests, coverage collection, and Swagger in Development.

Not implemented in application code:

- Receiving, sending, transforming, retrying, replaying, or monitoring integration messages.
- Kafka, Redis, Azurite, or external-system client integration.
- Connector credentials/secrets; authentication, authorization, tenancy, audit, health checks, metrics, tracing, and structured business-event logging.
- External-system update/activation/deactivation; connector/workflow deactivation; API versioning; pagination/filtering; a frontend project.

The README mentions Angular and Kafka, but this workspace contains no frontend project and no Kafka client code. Kafka, Redis, and Azurite occur only in local Docker infrastructure.

## Architecture and Solution Composition

```text
HTTP / OpenAPI
        |
        v
API: controllers, request binding, response status mapping, DI composition
        |
        v
Application: commands, queries, handlers, repository interfaces, response models
        |
        v
Domain: entities, state transitions, workflow-step rules, enums
        ^
        |
Infrastructure: EF Core DbContext, mappings, migrations, repository implementations
        |
        v
SQL Server
```

This realizes the modular-monolith and vertical-slice decisions in [architecture-decisions.md](architecture-decisions.md). The actual project graph is:

| Project                                         | Role                                             | Direct project references          |
| ----------------------------------------------- | ------------------------------------------------ | ---------------------------------- |
| `EnterpriseIntegrationHub.Api`                  | ASP.NET Core host and HTTP boundary              | Application, Infrastructure        |
| `EnterpriseIntegrationHub.Application`          | Use cases and persistence ports                  | Domain                             |
| `EnterpriseIntegrationHub.Domain`               | Business model                                   | None                               |
| `EnterpriseIntegrationHub.Infrastructure`       | EF Core persistence adapters                     | Domain, Application                |
| `EnterpriseIntegrationHub.SharedKernel`         | Included in solution; currently only `Class1.cs` | None                               |
| Domain/Application/Infrastructure test projects | Automated verification                           | Respective implementation projects |

All production projects target `net9.0`, enable nullable reference types and implicit usings. EF Core SQL Server and EF tooling are currently referenced by Domain, Application, and Infrastructure projects; actual EF persistence implementation is in Infrastructure.

| Area           | Implemented contents                                                                                   |
| -------------- | ------------------------------------------------------------------------------------------------------ |
| API            | `Program`, `Startup`, three controllers, request DTOs, Swagger extension, settings, HTTP scratch file. |
| Application    | Feature-slice commands, queries, handlers, validators, response models, three repository interfaces.   |
| Domain         | `BaseEntity`, ExternalSystem, Connector, Workflow, WorkflowStep/definition, six enums.                 |
| Infrastructure | DbContext, four EF configurations, three repositories, four migrations plus model snapshot.            |
| Tests          | Domain, Application, and Infrastructure projects; 16 test classes.                                     |
| Automation     | GitHub Actions CI for format, build, test, coverage, and artifacts.                                    |

## HTTP API Contract

All endpoints are below `/api`, use JSON, and are in [ExternalSystemsController.cs](../../backend/src/EnterpriseIntegrationHub.Api/Controllers/ExternalSystemsController.cs), [ConnectorsController.cs](../../backend/src/EnterpriseIntegrationHub.Api/Controllers/ConnectorsController.cs), and [WorkflowsController.cs](../../backend/src/EnterpriseIntegrationHub.Api/Controllers/WorkflowsController.cs). Identifier routes require GUIDs.

### External Systems

| Method and route                 | Request                              | Success response                                               | Implemented behavior                                      |
| -------------------------------- | ------------------------------------ | -------------------------------------------------------------- | --------------------------------------------------------- |
| `POST /api/external-systems`     | `Name`, `Description`, `Environment` | `201 Created`, `{ id }`; Location `/api/external-systems/{id}` | Checks unique `(Name, Environment)` and creates `Active`. |
| `GET /api/external-systems`      | None                                 | `200 OK`, `ExternalSystemsResponseModel`                       | Name-ordered summaries plus `TotalCount`.                 |
| `GET /api/external-systems/{id}` | None                                 | `200 OK`, `ExternalSystemSummary`                              | Returns one system; missing IDs return `404`.             |

Create requests require name/max 200 and description/max 1000 through data annotations. `Environment` binds as an enum without explicit defined-enum validation.

### Connectors

| Method and route                     | Request                                                                       | Success response                    | Implemented behavior                                                                                                                                 |
| ------------------------------------ | ----------------------------------------------------------------------------- | ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `POST /api/connectors`               | Name, description, external-system ID, base URL, protocol, auth type, timeout | `201 Created`, `{ id }`             | Requires existing Active external system and unique `(ExternalSystemId, Name)`; creates Draft.                                                       |
| `GET /api/connectors`                | None                                                                          | `200 OK`, `ConnectorsResponseModel` | Name-ordered summaries plus `TotalCount`.                                                                                                            |
| `GET /api/connectors/{id}`           | None                                                                          | `200 OK`, `ConnectorSummary`        | Returns one connector; missing IDs return `404`.                                                                                                     |
| `PUT /api/connectors/{id}`           | Full replacement-shaped connector payload excluding owner                     | `204 No Content`                    | Requires active owner and unique name. Changed endpoint/protocol/auth type/timeout turns Active into Draft; basic-info-only changes preserve status. |
| `POST /api/connectors/{id}/activate` | None                                                                          | `204 No Content`                    | Requires existing Draft connector and Active owner.                                                                                                  |

Create requests require all properties, URL format, and timeout `1..3600`. Update requests apply max-length/URL/range rules but do not mark fields required; the application validator nevertheless requires nonblank name, HTTP/HTTPS URL, and positive timeout. This is current behavior and a review item.

### Workflows

| Method and route                    | Request                                                             | Success response                         | Implemented behavior                                                               |
| ----------------------------------- | ------------------------------------------------------------------- | ---------------------------------------- | ---------------------------------------------------------------------------------- |
| `POST /api/workflows`               | Name, source connector ID, trigger event, one or more ordered steps | `201 Created`, `{ id }`                  | Creates Draft after checking unique name and active source/destination connectors. |
| `GET /api/workflows`                | None                                                                | `200 OK`, `WorkflowsResponseModel`       | Name-ordered summaries, including `StepCount`.                                     |
| `GET /api/workflows/{id}`           | None                                                                | `200 OK`, `WorkflowDetailsResponseModel` | Returns details and execution-order-sorted steps; missing IDs return `404`.        |
| `PUT /api/workflows/{id}`           | Full replacement-shaped workflow payload                            | `204 No Content`                         | Draft only; replaces steps after validating all connectors are Active.             |
| `POST /api/workflows/{id}/activate` | None                                                                | `204 No Content`                         | Draft only; requires all referenced connectors still Active.                       |

Workflow create/update requests require name/max 200, trigger event/max 200, at least one step, and positive step execution order. Empty source/destination IDs are not directly rejected by data annotations.

### Response Shapes and Error Contract

| Response model                 | Shape                                                                                                     |
| ------------------------------ | --------------------------------------------------------------------------------------------------------- |
| `ExternalSystemsResponseModel` | `Items: ExternalSystemSummary[]`, `TotalCount`; summary is ID, name, description, environment, status.    |
| `ConnectorsResponseModel`      | `Items: ConnectorSummary[]`, `TotalCount`; summary includes all connector configuration and status.       |
| `WorkflowsResponseModel`       | `Items: WorkflowSummary[]`, `TotalCount`; summary includes source, trigger event, status, and step count. |
| `WorkflowDetailsResponseModel` | Workflow summary fields plus `Steps: { DestinationConnectorId, ExecutionOrder }[]`.                       |

| Exception                           | HTTP status       | Body                                    |
| ----------------------------------- | ----------------- | --------------------------------------- |
| `ArgumentException`                 | `400 Bad Request` | `{ "message": "..." }`                  |
| `KeyNotFoundException`              | `404 Not Found`   | `{ "message": "..." }`                  |
| `InvalidOperationException`         | `409 Conflict`    | `{ "message": "..." }`                  |
| Model binding/annotation validation | `400 Bad Request` | ASP.NET Core `ValidationProblemDetails` |

Controllers perform this exception mapping locally. There is no centralized exception middleware or a normalized custom error contract.

## Application Use Cases

The Application layer uses manual immutable command/query records and handlers, not MediatR. Handlers take cancellation tokens and depend on repository interfaces. Creates return IDs, reads return response models, and update/state commands return `Task`.

| Module           | Commands and handlers                                                                        | Queries and handlers                                           |
| ---------------- | -------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| External systems | `CreateExternalSystemCommand`, handler, validator                                            | `BrowseExternalSystemsQuery`; `ViewExternalSystemDetailsQuery` |
| Connectors       | Create command/handler/validator; Update command/handler/validator; Activate command/handler | Browse query; ViewDetails query                                |
| Workflows        | Create command plus step command; Update command plus step command; Activate command/handler | Browse query; ViewDetails query                                |

Implementation detail:

- Connector create/update validators are manually instantiated inside handlers. Both require nonblank name, a valid absolute HTTP/HTTPS URL, and positive timeout. Create additionally rejects an empty external-system ID.
- `CreateExternalSystemCommandValidator` checks only a nonblank name. API annotations and EF mappings carry description requirements/lengths.
- Workflow construction carries most workflow structural validation; handlers add name-uniqueness and active-connector checks.
- Read handlers map entities to response records; API controllers never return domain objects.
- Connector/workflow update handlers call repository update even when aggregate methods find no changed value.

### Repository Ports

| Port                        | Operations                                                                                                            |
| --------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `IExternalSystemRepository` | `ExistsAsync(name, environment)`, `AddAsync`, `GetAllAsync`, `GetByIdAsync`                                           |
| `IConnectorRepository`      | `ExistsAsync(externalSystemId, name, excludedConnectorId?)`, `AddAsync`, `GetAllAsync`, `GetByIdAsync`, `UpdateAsync` |
| `IWorkflowRepository`       | `ExistsByNameAsync(name, excludedWorkflowId?)`, `AddAsync`, `GetByIdAsync`, `UpdateAsync`, `GetAllAsync`              |

## Domain Model and Business Rules

### Shared State

`BaseEntity` assigns a new GUID and `CreatedAt = UtcNow` in its protected constructor. `Id` and `CreatedAt` have private initialization; derived types can set nullable `UpdatedAt`.

### ExternalSystem

State: name, description, environment, status. The create handler supplies initial `Active` status. There are no domain methods to update or transition an external system. `ExternalSystemStatus` is `Active = 1`, `Inactive = 2`; environment is Development, Testing, UserAcceptance, and Production with values `1..4`.

### Connector

State: owning external-system ID, name/description, base URL, protocol, authentication type, timeout, and status.

| Operation             | Enforced behavior                                                                                                                      |
| --------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| Construction          | Directly assigns supplied values. Essential create validation is currently in Application, not the constructor.                        |
| `UpdateBasicInfo`     | Rejects blank name; returns false if unchanged; otherwise changes name and description.                                                |
| `UpdateCommunication` | Rejects blank URL/nonpositive timeout; returns false if unchanged; otherwise updates communication. An Active connector becomes Draft. |
| `MarkUpdated`         | Sets `UpdatedAt` to UTC now.                                                                                                           |
| `Activate`            | Requires Draft, then sets Active and `UpdatedAt`.                                                                                      |

Enums: status `Draft = 1`, `Active = 2`, `Inactive = 3`; protocol `REST = 1`, `SOAP = 2`, `SFTP = 3`, `GraphQL = 4`; authentication `APIKey = 1`, `OAuth2 = 2`, `Basic = 3`, `BearerToken = 4`.

### Workflow Aggregate

State: name, source connector ID, trigger event, status, and owned `WorkflowStep` collection. Its constructor requires a nonblank name and trigger event, at least one step, positive and unique execution orders, and no destination equal to the source. It initializes Draft.

`Update` is Draft-only, builds a replacement to reapply constructor rules, compares scalar fields and ordered destination/order pairs, and on change replaces steps and updates `UpdatedAt`. `Activate` is Draft-only and sets Active/`UpdatedAt`. `WorkflowStep` contains workflow ID, destination connector ID, and execution order. `WorkflowStepDefinition` is the immutable constructor input. Workflow status is `Draft = 0`, `Active = 1`, `Inactive = 2`.

## Persistence Model

`EnterpriseIntegrationHubDbContext` has sets for external systems, connectors, workflows, and workflow steps; it discovers configurations from the Infrastructure assembly. Repositories use `AsNoTracking()` for reads and `DbSet.Update()` for disconnected aggregate updates. Add/update operations perform one `SaveChangesAsync` call.

| Table             | Keys and constraints                                                                                                               | Relationships                                                             |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| `ExternalSystems` | App-generated GUID key; unique case-insensitive `(Name, Environment)`; name max 200; description max 1000.                         | Referenced by connectors.                                                 |
| `Connectors`      | App-generated GUID key; unique case-insensitive `(Name, ExternalSystemId)`; name max 200; description max 1000; base URL max 1000. | Required external-system FK; no configured cascade delete.                |
| `Workflows`       | App-generated GUID key; unique case-insensitive name; trigger event max 200.                                                       | Source connector FK uses `DeleteBehavior.Restrict`; owns steps.           |
| `WorkflowSteps`   | Composite primary key `(WorkflowId, ExecutionOrder)`.                                                                              | Workflow FK cascades; destination connector FK is restricted and indexed. |

Enums persist as integers. Checked-in migrations are `InitialCreate`, `AddDomainModelForConnector`, `AddWorkflows`, and `SyncWorkflowModelSnapshot`, plus the model snapshot.

## Runtime Composition and Configuration

`Program.cs` uses the `Startup` pattern. `Startup`:

- Registers controllers, Swagger/OpenAPI, and a SQL Server DbContext using `ConnectionStrings:DefaultConnection`.
- Registers repository implementations and scoped handlers, including `UpdateConnectorHandler`. This registration is an uncommitted correction relative to `846cd68`; commit it and add an API-host regression test. See AR-001 in the companion review.
- Enables a `Default` CORS policy allowing every origin, method, and header.
- Maps controllers. It does not configure authentication, authorization, HTTPS redirection, exception middleware, health checks, or migration-on-startup.
- Serves Swagger UI at `/swagger` in Development only. The OpenAPI document is named `v1`; HTTP routes do not include a version segment.

`appsettings.Development.json` contains a local SQL Server connection string with a development password. It is a local-development choice, not a production secret-management pattern.

## Tests and Continuous Integration

| Test project         | Approach                       | Coverage                                                                                                             |
| -------------------- | ------------------------------ | -------------------------------------------------------------------------------------------------------------------- |
| Domain tests         | xUnit + FluentAssertions       | Connector update/activation behavior; workflow construction, order rules, activation, and replacement update.        |
| Application tests    | xUnit + Moq + FluentAssertions | Create, browse, view, update, and activate handlers across all three modules, including major success/failure paths. |
| Infrastructure tests | xUnit + EF Core InMemory       | Connector repository add/read/exists/order and detached-query behavior.                                              |

There are 16 test classes. There are no API-host/controller, SQL Server relational, migration, DI-composition, or end-to-end configuration-flow tests. EF InMemory does not validate SQL Server foreign keys, collation, unique indexes, or migrations.

Last verified command and result:

```powershell
Set-Location backend
dotnet test EnterpriseIntegrationHub.sln --no-restore --verbosity minimal
```

**78 passed, 0 failed, 0 skipped** on this baseline.

[ci.yml](../../.github/workflows/ci.yml) runs on changes to `backend/**` or its own file. It restores using .NET 9/NuGet caching, verifies formatting, builds Release with warnings as errors, runs tests with Coverlet coverage/TRX output, generates HTML and Cobertura reports with ReportGenerator, and uploads test/coverage artifacts. Branch protection, a coverage threshold/badge, deployment, and release automation are not part of this workflow.

## Local Infrastructure and Deployment Boundary

[docker-compose.yml](../../infrastructure/docker-compose.yml) defines the `eih-infra` bridge network and persistent volumes.

| Service         | Port(s)                      | Current application use              |
| --------------- | ---------------------------- | ------------------------------------ |
| SQL Server 2022 | 1433                         | Used by configured EF Core provider. |
| Redis 7.2       | 6379                         | No client/cache use yet.             |
| Azurite         | 10000-10002                  | No storage use yet.                  |
| Redpanda/Kafka  | 9092, 29092; `kafka` profile | No producer/consumer use yet.        |

There is no API container image, API compose service, or deployment manifest. Local startup needs reachable SQL Server and an explicit migration step; the host does not migrate automatically.

## Reviewer Prompts and Maintenance

For architecture questions, use [implementation-baseline-review.md](implementation-baseline-review.md), especially AR-001 through AR-012. The main review decisions are aggregate invariant ownership, published configuration/concurrency strategy, stable error contracts, relational/API test coverage, and the security/operability model required before execution.

Update this dossier in the same pull request as any major capability change. Revise scope, routes/contracts/errors, domain rules, persistence/migrations, runtime registrations, test evidence, and the companion review finding status. This document intentionally records known defects and temporary choices so reviewers evaluate the actual foundation rather than an intended one.
