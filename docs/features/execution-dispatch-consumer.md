# Execution dispatch consumer

The optional Kafka consumer validates each dispatch and stores a durable inbox row before committing its Kafka offset. The stored dispatch and execution must match the database's outbox and execution metadata. The dispatch ID and execution ID are unique in the inbox, so redelivery, concurrent consumers, and a crash after storage but before offset commit do not create duplicate work.

The inbox is a handoff, not an execution worker. No destination is invoked by this slice, and a stored dispatch does not mean that an execution succeeded. A [lease primitive](execution-worker-leases.md) supports worker ownership; the future worker must handle old receipts without payloads explicitly and implement fenced step execution and recovery.

## Configuration and operation

Apply the database migrations before enabling the consumer. Set `Kafka__ConsumerEnabled=true`; it is disabled by default. Configure `Kafka__BootstrapServers`, `Kafka__ExecutionDispatchTopic`, and `Kafka__ExecutionDispatchConsumerGroup` for the deployment. Members of the same group share partitions. Preserve the group and the SQL inbox together when recovering a deployment.

Offset auto-commit and auto-store are disabled. The consumer handles one message at a time and commits only after the inbox write succeeds. A persistence or Kafka failure stops the host; restart it after resolving the error. An invalid or mismatched message also stops the host without committing that offset. Inspect and remediate the problematic topic/partition/offset operationally; do not skip it without accounting for the execution. Kafka messages may contain identifiers and should be handled as sensitive operational data.

The dispatch correlation ID is validated against the Kafka header and retained on the execution. Distributed trace-context propagation is not yet implemented.

## Tests

The SQL Server tests exercise duplicate delivery and competing inbox inserts. Record processor tests verify commit ordering and no commit on invalid messages, failed persistence, or cancellation. Broker integration tests exercise the hosted consumer and real producer/consumer against SQL Server. They create and delete unique test topics and require a Kafka broker at `EIH_KAFKA_TEST_BOOTSTRAP`; without that environment variable they are explicitly skipped.
