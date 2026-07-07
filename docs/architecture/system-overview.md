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

## Current Module

### External Systems

Current functionality:

- Register External System
- Prevent duplicate registrations
- Persist External System

Future modules:

- Connectors
- Workflows
- Transformations
- Integration Executions