using EnterpriseIntegrationHub.Api.Contracts.Requests;
using EnterpriseIntegrationHub.Application.Features.Workflows.Create;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseIntegrationHub.Api.Controllers;

[ApiController]
[Route("api/workflows")]
[Produces("application/json")]
public sealed class WorkflowsController(CreateWorkflowHandler createHandler) : ControllerBase
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
}
