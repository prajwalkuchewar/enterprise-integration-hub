# Enterprise Integration Hub

An integration management workspace built as a monorepo. The backend is an ASP.NET Core API with SQL Server persistence; the frontend is an Angular application consuming the API for external-system, connector, and workflow management.

## Repository layout

```text
backend/       ASP.NET Core API, application/domain/infrastructure projects, tests
frontend/      Angular workspace and web application
infrastructure/ Docker Compose services for local development
docs/          Product, architecture, and feature notes
```

## Start the application locally

1. Start SQL Server from the repository root with `docker compose -f infrastructure/docker-compose.yml up -d eih-sqlserver`.
2. Apply the backend database migrations with `dotnet ef database update --project backend/src/EnterpriseIntegrationHub.Infrastructure --startup-project backend/src/EnterpriseIntegrationHub.Api`.
3. Start the API with `dotnet run --project backend/src/EnterpriseIntegrationHub.Api --launch-profile http`.
4. Install the JavaScript workspace dependencies once with `npm install`.
5. Start Angular from the repository root with `npm run start` and open `http://localhost:4200`.

The Angular development server proxies `/api` requests to `http://localhost:5194`. Swagger is available from the API at `http://localhost:5194/swagger` while running in Development. Install the `dotnet-ef` tool if the `dotnet ef` command is not available.

To create a production frontend bundle, run `npm run build`. The output is written under `frontend/dist/`.

## REST destination methods

REST connectors support POST, PUT, and PATCH with the event JSON as the request
body. POST is the default for existing connectors and previously created
execution steps. The selected method is stored with each execution step, so
later connector changes do not alter an accepted execution. Changing the method
on an active connector returns it to Draft. GET and DELETE are not supported.

Destination delivery is opt-in and requires the outbox, Kafka consumer, and
execution worker to be enabled, an allowed destination origin, and credentials.
Connector updates from older clients that omit the method preserve its current
value. Apply migrations before starting this version.

## Current frontend scope

- Overview of registered systems, connectors, and workflows.
- Create, edit, activate, and deactivate external systems and connectors.
- Create, edit, and activate workflows with ordered destination steps.
- Searchable resource lists with API error and empty states.

The backend roadmap tracks execution, transformations, monitoring, audit history, and authentication as future product work.
