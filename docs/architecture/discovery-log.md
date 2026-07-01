# Enterprise Integration Hub (EIH)

## Architecture Discovery Log — Session 1

**Project Stage:** Sprint 1 - Domain Discovery

---

# 1. Project Vision

The Enterprise Integration Hub (EIH) is an enterprise platform responsible for enabling secure, reliable, observable, and standardized exchange of business events between independent enterprise systems.

The platform does **not** own business data. Instead, it owns the process of moving business information between systems while ensuring operational visibility, reliability, and consistency.

---

# 2. Business Problem

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

# 3. Core Mission

> Move business events safely, reliably, observably, and consistently between enterprise systems.

---

# 4. Responsibilities of the Platform

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

# 5. Responsibilities the Platform Does NOT Own

The Enterprise Integration Hub is **not** the source of truth for business data.

Business ownership remains with the originating systems.

Examples:

- HRMS owns employees.
- ERP owns invoices.
- Payroll owns salary information.
- CRM owns customers.

The Integration Hub only manages the movement of this information.

---

# 6. Important Architectural Lessons

## Lesson 1

Software exists to solve business problems.

Technology is chosen only after the business problem is understood.

Business → Software → Technology

---

## Lesson 2

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

## Lesson 3

Responsibilities create modules.

We should never invent modules first.

Business responsibilities naturally lead to software modules.

---

## Lesson 4

The Integration Hub is the operational owner of information exchange.

It is **not** the owner of business information.

---

# 7. Domain Concepts Discovered

## Connector

### Responsibility

Represents everything required for the Integration Hub to communicate with a single external system.

A Connector knows:

- Base URL
- Authentication
- Timeout
- Retry Policy
- Supported capabilities
- Connection status

A Connector does **not** contain workflow logic or execution history.

---

## Workflow

### Responsibility

Defines how business events move through the organization.

A Workflow decides:

- Which event starts the process.
- Which Connector is used.
- Which destination systems receive the event.
- What transformations or routing rules apply.

The Workflow orchestrates communication.

---

## Integration Execution

### Responsibility

Represents one execution attempt of a workflow.

Each execution records:

- Start time
- End time
- Current status
- Retry count
- Correlation ID
- Processing duration
- Failure information (if any)

Executions are created every time a workflow runs.

---

## Audit Log

### Responsibility

Stores evidence of what happened during an Integration Execution.

Audit Logs support:

- Troubleshooting
- Compliance
- Operational visibility
- Historical investigation

An Audit Log is evidence, not the execution itself.

---

# 8. Domain Relationships (Current Understanding)

Connector

↓

Workflow

↓

Integration Execution

↓

Audit Log

Each concept owns a distinct responsibility and evolves independently.

---

# 9. Design Principles Established

- Design from business problems, not technology choices.
- Business capabilities define system modules.
- Every module should own a single responsibility.
- Separate long-lived configuration from short-lived execution data.
- Business systems remain the source of truth.
- The Integration Hub owns integration operations.
- Every architectural decision should have a business justification.

---

# 10. Open Questions

These will guide future design sessions.

- What is the lifecycle of a Connector?
- How are Workflows modeled and versioned?
- What business events should the platform support initially?
- What defines a successful Integration Execution?
- When should retries occur?
- How should failed executions be replayed?
- What metrics should Operations monitor?

---

# Current Domain Model

```
Enterprise Integration Hub

├── Connector
│      └── Knows how to communicate
│
├── Workflow
│      └── Knows when and where to communicate
│
├── Integration Execution
│      └── Represents one execution attempt
│
└── Audit Log
       └── Records what happened
```

---

## Sensei's Rule #1

**Responsibilities before Components.**

Never ask:

> "What controller should I build?"

Instead ask:

> "What responsibility does the business need fulfilled?"

The software design should emerge naturally from the answer.
