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