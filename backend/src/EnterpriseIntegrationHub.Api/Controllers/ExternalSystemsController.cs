using EnterpriseIntegrationHub.Api.Contracts.Requests;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Browse;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Create;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.ViewDetails;
using EnterpriseIntegrationHub.Application.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseIntegrationHub.Api.Controllers;

/// <summary>
/// Controller for managing external systems.
/// </summary>
[ApiController]
[Route("api/external-systems")]
[Produces("application/json")]
public sealed class ExternalSystemsController : ControllerBase
{
    private readonly CreateExternalSystemHandler _createHandler;
    private readonly BrowseExternalSystemsHandler _browseHandler;
    private readonly ViewExternalSystemDetailsHandler _viewDetailsHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalSystemsController"/> class.
    /// </summary>
    public ExternalSystemsController(
        CreateExternalSystemHandler createHandler,
        BrowseExternalSystemsHandler browseHandler,
        ViewExternalSystemDetailsHandler viewDetailsHandler)
    {
        _createHandler = createHandler;
        _browseHandler = browseHandler;
        _viewDetailsHandler = viewDetailsHandler;
    }

    /// <summary>
    /// Creates a new external system.
    /// </summary>
    /// <param name="request">Create request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new resource id, or 409 Conflict if duplicate.</returns>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateExternalSystemRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateExternalSystemCommand(
            request.Name,
            request.Description, 
            request.Environment);

        try
        {
            var id = await _createHandler.Handle(command, cancellationToken);
            return Created($"/api/external-systems/{id}", new { id });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    /// <summary>
    /// Browse external systems.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a list of external system summaries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ExternalSystemsResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Browse(CancellationToken cancellationToken)
    {
        var query = new BrowseExternalSystemsQuery();
        var response = await _browseHandler.Handle(query, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// View external system detail by id.
    /// </summary>
    /// <param name="id">External system id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with details, or 404 NotFound if the id does not exist.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExternalSystemSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewDetails([FromRoute] Guid id, CancellationToken cancellationToken)
    {
            var query = new ViewExternalSystemDetailsQuery(id);
        try
        {
            var response = await _viewDetailsHandler.Handle(query, cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
