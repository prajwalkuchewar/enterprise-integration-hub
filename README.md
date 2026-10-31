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
2. Start the API with `dotnet run --project backend/src/EnterpriseIntegrationHub.Api --launch-profile http`.
3. Install the JavaScript workspace dependencies once with `npm install`.
4. Start Angular from the repository root with `npm run start` and open `http://localhost:4200`.

The Angular development server proxies `/api` requests to `http://localhost:5194`. Swagger is available from the API at `http://localhost:5194/swagger` while running in Development.

To create a production frontend bundle, run `npm run build`. The output is written under `frontend/dist/`.

## Current frontend scope

- Overview of registered systems, connectors, and workflows.
- Create, edit, activate, and deactivate external systems and connectors.
- Create, edit, and activate workflows with ordered destination steps.
- Searchable resource lists with API error and empty states.

The backend roadmap tracks execution, transformations, monitoring, audit history, and authentication as future product work.
