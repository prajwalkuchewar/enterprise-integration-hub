using EnterpriseIntegrationHub.Api.Contracts.Requests;
using EnterpriseIntegrationHub.Application.Features.Events.Ingest;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseIntegrationHub.Api.Controllers;

/// <summary>
/// Webhook endpoint that accepts events from registered source systems.
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public sealed class EventsController(IngestEventHandler ingestEventHandler) : ControllerBase
{
    private const string IngestionKeyHeader = "X-Ingestion-Key";

    /// <summary>
    /// Accepts an event for durable processing. The event is not reported as processed until its execution completes.
    /// </summary>
    /// <param name="ingestionKey">Connector ingestion key sent in the <c>X-Ingestion-Key</c> header.</param>
    /// <param name="request">Event envelope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// 202 Accepted for a new event, 200 OK when the same event content was already accepted,
    /// 400 for invalid envelopes, 401 for missing or unknown keys, 404 when no workflow matches, and 409 when the event ID was reused with different content.
    /// </returns>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ingest(
        [FromHeader(Name = IngestionKeyHeader)] string? ingestionKey,
        [FromBody] IngestEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await ingestEventHandler.Handle(
            new IngestEventCommand(
                ingestionKey ?? string.Empty,
                request.EventId,
                request.EventType,
                request.SourceSystem,
                request.OccurredAt,
                request.CorrelationId,
                request.SchemaVersion,
                request.Payload),
            cancellationToken);

        var body = new
        {
            executionId = result.ExecutionId,
            status = result.Status.ToString(),
            alreadyAccepted = result.AlreadyExisted,
            message = result.AlreadyExisted
                ? "Event was already accepted. No new execution was created."
                : "Event accepted for durable processing."
        };

        return result.AlreadyExisted ? Ok(body) : Accepted(body);
    }
}
