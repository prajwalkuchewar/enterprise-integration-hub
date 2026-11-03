# Execution worker ownership leases

The execution dispatch inbox has a SQL Server lease primitive for a future execution worker. `ClaimAsync` atomically claims one eligible inbox entry using row locks and returns a unique token. Expired claims can be reclaimed. `RenewAsync`, `ReleaseAsync`, and `CompleteAsync` only succeed while that token still owns an unexpired lease. SQL Server's clock determines lease expiry and claim eligibility, avoiding differences between worker clocks.

Release may set an availability time for future scheduling. Completion also requires the associated execution to be in a terminal state. The migration leaves existing inbox entries available for claiming. Lease durations are specified in whole seconds and must be between 1 and 86400.

This is an ownership primitive, not a running execution worker. The worker must fence execution and step state changes transactionally against the lease, renew or cancel long-running work safely, recover interrupted attempts, and ensure the lease remains valid before starting an external call. A lease does not by itself prevent an external destination from processing a request after a lease expires or a network failure becomes ambiguous. Stable step idempotency keys and destination support are still required; exactly-once side effects cannot be guaranteed.

SQL Server integration tests cover concurrent claims (including read-committed snapshot isolation), expiry and reclaim, stale-token fencing, delayed release, renewal, completion and input validation.
