# Feature

Create External System

---

## Business Context

Before an organization can configure integrations, the Integration Hub must know which enterprise systems exist.

This feature allows an Integration Administrator to register a new External System.

No communication configuration is performed in this feature.

---

## Primary User

Integration Administrator

---

## User Journey

1. Open External Systems.
2. Click Create.
3. Enter Name.
4. Enter Description.
5. Select Environment.
6. Save.
7. External System is registered.
8. User is redirected back to the list.
9. User is encouraged to configure a Connector next.

---

## Business Rules

- Name is required.
- Description is required.
- Environment is required.
- Name must be unique within the selected environment.
- Newly created systems are Active by default.

---

## Technical Flow

```
HTTP Request

↓

ExternalSystemsController

↓

CreateExternalSystemHandler

↓

IExternalSystemRepository

↓

ExternalSystemRepository

↓

SQL Server
```

---

## Implemented Components

### Domain

- ExternalSystem
- BaseEntity
- ExternalSystemEnvironment
- ExternalSystemStatus

### Application

- CreateExternalSystemCommand
- CreateExternalSystemHandler
- IExternalSystemRepository

### Infrastructure

- EnterpriseIntegrationHubDbContext
- ExternalSystemRepository
- ExternalSystemConfiguration

### API

- ExternalSystemsController
- CreateExternalSystemRequest

---

## Validation

Current implementation prevents duplicate registrations by checking:

Name + Environment

---

## Lessons Learned

This feature established the project's first complete vertical slice.

Key learnings:

- Separate HTTP concerns from business logic.
- Keep business rules inside Handlers.
- Repository interfaces belong to Application.
- Infrastructure owns persistence.
- Build features around business capabilities rather than technical layers.