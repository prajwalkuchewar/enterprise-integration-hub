using EnterpriseIntegrationHub.Api.Contracts.Requests;
using EnterpriseIntegrationHub.Application.Features.Workflows.Create;
using EnterpriseIntegrationHub.Application.Features.Workflows.Update;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseIntegrationHub.Api.Controllers;

[ApiController]
[Route("api/workflows")]
[Produces("application/json")]
public sealed class WorkflowsController(CreateWorkflowHandler createHandler, UpdateWorkflowHandler updateHandler) : ControllerBase
{
    /// <summary>Creates a draft workflow that routes a trigger event from one connector to one or more destinations.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var steps = request.Steps.Select(x => new CreateWorkflowStepCommand(x.DestinationConnectorId, x.ExecutionOrder)).ToArray();
            var id = await createHandler.Handle(new CreateWorkflowCommand(request.Name, request.SourceConnectorId, steps, request.TriggerEvent), cancellationToken);
            return Created($"/api/workflows/{id}", new { id });
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }

    /// <summary>Updates a draft workflow and replaces its ordered steps.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateWorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var steps = request.Steps.Select(x => new UpdateWorkflowStepCommand(x.DestinationConnectorId, x.ExecutionOrder)).ToArray();
            await updateHandler.Handle(id, new UpdateWorkflowCommand(request.Name, request.SourceConnectorId, steps, request.TriggerEvent), cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        catch (KeyNotFoundException exception) { return NotFound(new { message = exception.Message }); }
        catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); }
    }
}
