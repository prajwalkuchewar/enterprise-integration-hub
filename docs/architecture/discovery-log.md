# Architecture Discovery Log — Session 1

**Project Stage:** Sprint 1 - Domain Discovery

## 1. Project Vision

The Enterprise Integration Hub (EIH) is an enterprise platform responsible for enabling secure, reliable, observable, and standardized exchange of business events between independent enterprise systems.

The platform does **not** own business data. Instead, it owns the process of moving business information between systems while ensuring operational visibility, reliability, and consistency.

---

## 2. Business Problem

Large organizations operate many independent systems such as:

- ERP
- CRM
- HRMS
- Payroll
- Accounting
- Warehouse
- Vendor Portals
- Government APIs
- Banking APIs

Without a centralized integration platform, every team builds direct integrations between systems, resulting in:

- Tight coupling
- Inconsistent implementation
- Difficult maintenance
- Limited observability
- Increased operational cost
- Duplicate integration logic

The Enterprise Integration Hub solves these problems by acting as a centralized integration platform.

---

## 3. Core Mission

> Move business events safely, reliably, observably, and consistently between enterprise systems.

---

## 4. Responsibilities of the Platform

The platform owns the integration process, not the business data.

Its responsibilities include:

- Standardizing communication between enterprise systems.
- Managing communication contracts with external systems.
- Routing business events to the correct destination.
- Monitoring every integration execution.
- Enforcing consistent authentication and security policies.
- Recording audit information.
- Providing retry and recovery mechanisms.
- Offering operational visibility into every integration.
- Reducing coupling between enterprise systems.

---

## 5. Responsibilities the Platform Does NOT Own

The Enterprise Integration Hub is **not** the source of truth for business data.

Business ownership remains with the originating systems.

Examples:

- HRMS owns employees.
- ERP owns invoices.
- Payroll owns salary information.
- CRM owns customers.

The Integration Hub only manages the movement of this information.

---

## 6. Important Architectural Lessons

### Lesson 1

Software exists to solve business problems.

Technology is chosen only after the business problem is understood.

Business → Software → Technology

---

### Lesson 2

Architects think in business capabilities.

Developers often think in:

- Controllers
- Services
- Databases

Architects think in:

- Register Systems
- Execute Workflows
- Monitor Integrations
- Recover Failures
- Audit Operations

Capabilities define the software.

---

### Lesson 3

Responsibilities create modules.

We should never invent modules first.

Business responsibilities naturally lead to software modules.

---

### Lesson 4

The Integration Hub is the operational owner of information exchange.

It is **not** the owner of business information.

---

## 7. Domain Concepts Discovered

### External System

#### Responsibility

Represents an enterprise application that participates in integrations.

Examples include:

- HRMS
- ERP
- CRM
- Payroll
- ExpensePro

An External System represents the business identity of an application.

It does not define how communication occurs. That responsibility belongs to one or more Connectors.

### Connector

#### Responsibility

Represents the communication contract required to interact with a single External System.

A Connector defines:

- Base URL
- Authentication
- Timeout
- Retry Policy
- Headers
- Certificates (optional)
- Communication Protocol
- Connection Status

A Connector knows how to communicate with an External System but does not define business workflows or store execution history.

### Workflow

#### Responsibility

Defines a business integration process.

A Workflow orchestrates the movement of business events from one source system to one or more destination systems.

A Workflow defines:

- Trigger Event
- Source Connector
- One or more Workflow Steps
- Execution Strategy (Sequential/Parallel - future)
- Activation Status

### Workflow Step

#### Responsibility

Represents a single integration action within a Workflow.

Each Workflow Step defines:

- Destination Connector
- Transformation
- Execution Order
- Retry Policy (future)
- Execution Condition (future)

A Workflow may contain one or many Workflow Steps.

### Transformation

#### Responsibility

Defines how business data is transformed from the source system schema into the destination system schema.

Transformation responsibilities include:

- Field Mapping
- Value Conversion
- Filtering
- Enrichment
- Validation
- Default Values

A Transformation focuses solely on data conversion and contains no workflow or communication logic.

### Integration Execution

#### Responsibility

Represents one execution attempt of a workflow.
An Integration Execution represents the runtime instance of a Workflow execution.

Every execution has its own lifecycle independent of the Workflow definition.

Each execution records:

- Start time
- End time
- Current status
- Retry count
- Correlation ID
- Processing duration
- Failure information (if any)

Executions are created every time a workflow runs.

### Audit Log

#### Responsibility

Stores evidence of what happened during an Integration Execution.

Audit Logs support:

- Troubleshooting
- Compliance
- Operational visibility
- Historical investigation

An Audit Log is evidence, not the execution itself.
Audit Logs are immutable historical records and should never be modified after creation.

---

## 8. Domain Relationships (Current Understanding)

```text


                                            Workflow
                                                    |
        ┌──────────────┴──────────────┐
        │                                                                                  │
 Source Connector                                                Workflow Step(s)
                                                                                              │
                                                      ┌─────────────┴───────┐
                                                         │                                                           │
                                            Destination Connector                          Transformation
                                                                                                              │
                                                                                                                ▼
                                                                                 Integration Execution
                                                                                                        │
                                                                                                        ▼
                                                                                                    Audit Log

External System
      │
      ▼
Connector(s)
```

Each concept owns a distinct responsibility and evolves independently.

---

## 9. Design Principles Established

- Design from business problems, not technology choices.
- Business capabilities define system modules.
- Every module should own a single responsibility.
- Separate long-lived configuration from short-lived execution data.
- Business systems remain the source of truth.
- The Integration Hub owns integration operations.
- Every architectural decision should have a business justification.

---

## 10. Open Questions

These will guide future design sessions.

- What is the lifecycle of a Connector?
- How are Workflows modeled and versioned?
- What business events should the platform support initially?
- What defines a successful Integration Execution?
- When should retries occur?
- How should failed executions be replayed?
- What metrics should Operations monitor?
- Can one Workflow have multiple destinations?
- Should Transformations be reusable across Workflows?
- Can a Workflow Step execute in parallel?
- Should Workflow Steps support conditional execution?
- How should failed Workflow Steps affect the remaining steps?
- How should Workflow versions be managed?

---

## Current Domain Model (v0.1)

```
Enterprise Integration Hub

├── External System
│      └── Business identity of an enterprise application
│
├── Connector
│      └── Communication contract
│
├── Workflow
│      └── Business orchestration
│
├── Workflow Step
│      └── One integration action
│
├── Transformation
│      └── Data conversion
│
├── Integration Execution
│      └── Runtime execution
│
└── Audit Log
       └── Historical evidence

```

---

The software design should emerge naturally from the answer.

## Domain Modeling Principles

1. External Systems own business data.
2. Connectors own communication configuration.
3. Workflows own business orchestration.
4. Transformations own data conversion.
5. Integration Executions own runtime state.
6. Audit Logs own historical evidence.
7. Configuration objects and runtime objects should always be separated.

# Session 2 — External System Module Completed

## New Concepts Introduced

- Commands
- Queries
- Handler Pattern
- Response Models
- Read Models
- Repository Pattern
- Unit Testing

---

## Architectural Decisions

The External System capability has been established as the first administrative module.

Three primary use cases have been implemented:

- Create External System
- Browse External Systems
- View External System Details

These use cases establish the project's standard vertical slice architecture.

Future capabilities should follow the same development approach.
