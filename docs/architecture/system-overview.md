# System Overview

## Architecture

```
API
│
▼
Application
│
▼
Domain
│
▼
Infrastructure
```

---

## Layer Responsibilities

### API

Responsible for:

- HTTP
- Routing
- Request / Response contracts

Contains no business logic.

---

### Application

Responsible for implementing business use cases.

Contains:

- Commands
- Handlers
- Repository Interfaces

---

### Domain

Represents the business itself.

Contains:

- Entities
- Enums
- Domain Rules

Has no dependency on ASP.NET Core or Entity Framework.

---

### Infrastructure

Responsible for external technologies.

Contains:

- EF Core
- SQL Server
- Repository implementations

---

## Current Modules

### External Systems

Status

✅ Complete (Sprint 1)

Implemented Use Cases

- Create External System
- Browse External Systems
- View External System Details

Future Enhancements

- Update
- Activate
- Deactivate

---

### Connector Management

Status

✅ Complete (Sprint 2)

Implemented Use Cases

- Create Draft Connector
- Browse Connectors
- View Connector Details
- Update Connector
- Activate Draft Connector

Key Concept

Connectors define the communication contract for an external system. Updating an active connector's communication settings returns it to Draft status so it can be reviewed and activated again.

---

### Workflow Management

Status

✅ Complete (Sprint 3)

Implemented Use Cases

- Create Draft Workflow
- Browse Workflows
- View Workflow Details with Steps
- Update Draft Workflow
- Activate Draft Workflow

Key Concept

Workflows route a trigger event from one source connector to one or more destination connectors through sequentially ordered steps. A workflow begins in Draft status and can be activated only when all referenced connectors are active.
