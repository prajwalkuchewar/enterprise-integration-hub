# Feature

Connector Management

---

## Business Context

An External System identifies an enterprise application. A connector defines the communication contract used to reach that system, including its endpoint, protocol, authentication method, and timeout.

Integration Administrators configure connectors before using them as workflow sources or destinations.

---

## Primary User

Integration Administrator

---

## Connector Lifecycle

1. Create a connector in Draft status for an active External System.
2. Review the connector's communication settings.
3. Activate the Draft connector.
4. Update the connector when required. Changing communication settings on an Active connector returns it to Draft status for review and reactivation.

---

## Business Rules

- A connector belongs to one External System, which must exist and be Active.
- Name and description are required.
- Name must be unique within its External System.
- Base URL is required, must be a valid URL, and must be at most 1000 characters.
- Authentication type must be one of `APIKey`, `OAuth2`, `Basic`, or `BearerToken`.
- Timeout must be from 1 to 3600 seconds.
- New connectors start in Draft status.
- Only Draft connectors can be activated.
- Updating an Active connector's base URL, protocol, authentication type, or timeout returns it to Draft status.

---

## API Endpoints

| Method | Route                           | Description                                          |
| ------ | ------------------------------- | ---------------------------------------------------- |
| POST   | `/api/connectors`               | Create a draft connector.                            |
| GET    | `/api/connectors`               | Browse connectors.                                   |
| GET    | `/api/connectors/{id}`          | View connector details.                              |
| PUT    | `/api/connectors/{id}`          | Update connector details and communication settings. |
| POST   | `/api/connectors/{id}/activate` | Activate a draft connector.                          |

---

## Create and Update Payload

```json
{
	"name": "HR API",
	"description": "REST connector for the HR system",
	"externalSystemId": "11111111-1111-1111-1111-111111111111",
	"baseUrl": "https://hr.example.com/api",
	"protocol": "REST",
	"authenticationType": "OAuth2",
	"timeoutSeconds": 30
}
```

Create requires `externalSystemId`. Update keeps the connector's existing owning External System, so its payload omits `externalSystemId` and otherwise uses the same fields.

A successful create request returns `201 Created` with the connector identifier. Successful updates and activations return `204 No Content`.

---

## Technical Flow

```
HTTP Request

↓

ConnectorsController

↓

Create, Browse, View Details, Update, or Activate Handler

↓

IConnectorRepository and IExternalSystemRepository

↓

Repository Implementations

↓

SQL Server
```

---

## Implemented Components

### Domain

- Connector
- ConnectorStatus
- ConnectorProtocol
- ConnectorAuthenticationType

### Application

- CreateConnectorCommand and CreateConnectorHandler
- BrowseConnectorsQuery and BrowseConnectorsHandler
- ViewConnectorDetailsQuery and ViewConnectorDetailsHandler
- UpdateConnectorCommand and UpdateConnectorHandler
- ActivateConnectorCommand and ActivateConnectorHandler
- IConnectorRepository

### API

- ConnectorsController
- CreateConnectorRequest
- UpdateConnectorRequest

---

## Error Handling

- `400 Bad Request`: invalid request data.
- `404 Not Found`: an unknown connector or External System identifier.
- `409 Conflict`: duplicate connector names within an External System, an inactive owning External System, or activation of a connector that is not Draft.
