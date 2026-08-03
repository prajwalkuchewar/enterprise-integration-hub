# Feature

Workflow Management

---

## Business Context

After connectors are configured, an Integration Administrator defines how a business event moves from one source connector to one or more destination connectors.

A workflow captures that routing as an ordered set of steps. It is configured in Draft status before it can be activated.

---

## Primary User

Integration Administrator

---

## Workflow Lifecycle

1. Create a workflow in Draft status.
2. Add one or more destination steps with unique positive execution orders.
3. Review or update the draft workflow as integration requirements change.
4. Activate the workflow when its source and destination connectors are active.

---

## Business Rules

- Name and trigger event are required and limited to 200 characters.
- A workflow must contain at least one step.
- Each step must have an execution order of 1 or greater.
- Execution order values must be unique within a workflow.
- A destination connector cannot be the same as the source connector.
- Only Draft workflows can be updated or activated.
- Activation requires the source connector and every destination connector to be active.

---

## API Endpoints

| Method | Route                          | Description                                    |
| ------ | ------------------------------ | ---------------------------------------------- |
| POST   | `/api/workflows`               | Create a draft workflow.                       |
| GET    | `/api/workflows`               | Browse workflows in name order.                |
| GET    | `/api/workflows/{id}`          | View a workflow with its ordered steps.        |
| PUT    | `/api/workflows/{id}`          | Update a draft workflow and replace its steps. |
| POST   | `/api/workflows/{id}/activate` | Activate a draft workflow.                     |

---

## Create and Update Payload

```json
{
	"name": "Employee Onboarding",
	"sourceConnectorId": "11111111-1111-1111-1111-111111111111",
	"triggerEvent": "Employee.Created",
	"steps": [
		{
			"destinationConnectorId": "22222222-2222-2222-2222-222222222222",
			"executionOrder": 1
		},
		{
			"destinationConnectorId": "33333333-3333-3333-3333-333333333333",
			"executionOrder": 2
		}
	]
}
```

A successful create request returns `201 Created` and the new workflow identifier. The same payload structure is used for updates. Successful update and activation requests return `204 No Content`.

---

## Technical Flow

```
HTTP Request

↓

WorkflowsController

↓

Create, Browse, View Details, Update, or Activate Handler

↓

IWorkflowRepository and IConnectorRepository

↓

Repository Implementations

↓

SQL Server
```

---

## Implemented Components

### Domain

- Workflow
- WorkflowStep
- WorkflowStatus
- WorkflowStepDefinition

### Application

- CreateWorkflowCommand and CreateWorkflowHandler
- BrowseWorkflowsQuery and BrowseWorkflowsHandler
- ViewWorkflowDetailsQuery and ViewWorkflowDetailsHandler
- UpdateWorkflowCommand and UpdateWorkflowHandler
- ActivateWorkflowCommand and ActivateWorkflowHandler
- IWorkflowRepository

### API

- WorkflowsController
- CreateWorkflowRequest
- UpdateWorkflowRequest

---

## Error Handling

- `400 Bad Request`: invalid input, including an empty name, no steps, invalid execution orders, or a source connector used as a destination.
- `404 Not Found`: an unknown workflow or connector identifier.
- `409 Conflict`: a duplicate workflow name, an inactive referenced connector, or an operation on a workflow that is not Draft.
