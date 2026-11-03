# Execution dispatch outbox

Accepted events create an `IntegrationExecution` and an `ExecutionDispatchOutbox` row in the same transaction. A background worker publishes each pending row to Kafka and records the outcome. Lease, retry, and dead-letter rules follow [execution-domain-decisions.md](../architecture/execution-domain-decisions.md).

## How it works

1. The worker polls every `Outbox:PollInterval`.
2. It claims up to `Outbox:BatchSize` dispatchable rows in one transaction. Claims use `UPDLOCK, READPAST`, so concurrent API instances never claim the same row. Each claim sets `LeaseOwner` and `LeaseExpiresAt`.
3. The lease commits before publishing, so no database transaction is held during Kafka calls.
4. Each row is published to the `Kafka:Topic` topic. The key is the execution ID. Headers are `dispatch-id`, `correlation-id`, and `event-type`.
5. On success the row is marked dispatched. Every update checks the lease owner, so a worker whose lease expired cannot overwrite a newer claim.
6. On failure the row records `LastError`, increments `AttemptCount`, and sets `NextAttemptAt` using capped exponential backoff. After `MaxAttempts` the row is dead-lettered (`DeadLetteredAt`).

Delivery is at-least-once. A crash between publish and mark can publish a message twice, so consumers must be idempotent on `dispatch-id` or `executionId`.

## Configuration

```json
"Outbox": {
  "Enabled": false,
  "BatchSize": 25,
  "PollIntervalSeconds": 2,
  "LeaseSeconds": 60,
  "MaxAttempts": 8,
  "BaseRetryDelaySeconds": 2,
  "MaxRetryDelaySeconds": 300
},
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "ClientId": "eih-api",
  "ExecutionDispatchTopic": "eih.execution.dispatch.v1",
  "MessageTimeoutMs": 30000
}
```

The worker is disabled by default. Enable it with `Outbox__Enabled=true`.

## Running locally

```powershell
docker compose -f infrastructure\docker-compose.yml --profile kafka up -d eih-kafka
docker exec eih-kafka rpk cluster health

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Outbox__Enabled = 'true'
dotnet run --no-launch-profile --project backend\src\EnterpriseIntegrationHub.Api
```

`ASPNETCORE_ENVIRONMENT` must be set when `--no-launch-profile` is used, because the connection string lives in `appsettings.Development.json`.

Check the output:

```powershell
docker exec eih-kafka rpk topic consume eih.execution.dispatch.v1 -n 1 -o start
```

## Verification

- Domain, Application, and API unit tests cover the lease, retry, and fencing rules.
- `SqlServerExecutionDispatchOutboxTests` runs against SQL Server through Testcontainers. It covers exclusive claims, expired-lease reclaim, fenced mark rejection, dead-lettering, and concurrent claims with no duplicates.
- A manual end-to-end run on the local stack: an accepted event produced one message on `eih.execution.dispatch.v1` with the expected key and headers.
