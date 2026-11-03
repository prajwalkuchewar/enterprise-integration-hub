# Execution worker ownership leases

The execution dispatch inbox has a SQL Server lease primitive for a future execution worker. `ClaimAsync` atomically claims one eligible inbox entry using row locks and returns a unique token. Expired claims can be reclaimed. `RenewAsync`, `ReleaseAsync`, and `CompleteAsync` only succeed while that token still owns an unexpired lease. SQL Server's clock determines lease expiry and claim eligibility, avoiding differences between worker clocks.

Release may set an availability time for future scheduling. Completion also requires the associated execution to be in a terminal state. The migration leaves existing inbox entries available for claiming. Lease durations are specified in whole seconds and must be between 1 and 86400.

This is not a running execution worker. The worker must renew or cancel long-running work safely and ensure the lease remains valid before starting an external call. A lease does not by itself prevent an external destination from processing a request after a lease expires or a network failure becomes ambiguous. The logical step ID is a stable idempotency key across attempts, but destination support is required and exactly-once side effects cannot be guaranteed.

Attempt creation and outcome changes are routed through the execution aggregate: steps run in order, successful steps cannot be restarted, and a scheduled retry cannot start before its due time. The attempt repository locks and checks the inbox lease in the same SQL transaction as each state change. It rechecks expiry before commit, persists a running attempt before returning work, and records the result and inbox transition atomically. A replacement owner records the prior in-flight attempt as interrupted before retrying it, subject to the supplied attempt limit. Existing receipts without payloads are failed explicitly instead of being sent.

Attempt work includes sensitive event data and must not be logged or serialized into diagnostics. Failure diagnostics use fixed messages rather than raw destination content. The repository accepts a retry time but does not calculate backoff or provide a scheduler, HTTP connector, hosted worker, replay, or dead-letter operator experience.

SQL Server integration tests cover concurrent claims (including read-committed snapshot isolation), expiry and reclaim, stale-token fencing, delayed release, renewal, ordered attempts, interrupted-attempt recovery, missing payloads, and transaction rollback.
