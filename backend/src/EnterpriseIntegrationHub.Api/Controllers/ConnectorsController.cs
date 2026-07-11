using EnterpriseIntegrationHub.Api.Contracts.Requests;
using EnterpriseIntegrationHub.Application.Features.Connectors.Activate;
using EnterpriseIntegrationHub.Application.Features.Connectors.Browse;
using EnterpriseIntegrationHub.Application.Features.Connectors.Create;
using EnterpriseIntegrationHub.Application.Features.Connectors.ViewDetails;
using EnterpriseIntegrationHub.Application.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using EnterpriseIntegrationHub.Application.Features.Connectors.Retire;

namespace EnterpriseIntegrationHub.Api.Controllers;

/// <summary>
/// Controller for managing Connectors.
/// </summary>
[ApiController]
[Route("api/connectors")]
[Produces("application/json")]
public sealed class ConnectorsController : ControllerBase
{
    private readonly CreateConnectorHandler _createHandler;
    private readonly BrowseConnectorsHandler _browseHandler;
    private readonly ViewConnectorDetailsHandler _viewDetailsHandler;
    private readonly ActivateConnectorHandler _activateHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorsController"/> class.
    /// </summary>
    public ConnectorsController(
        CreateConnectorHandler createHandler,
        BrowseConnectorsHandler browseHandler,
        ViewConnectorDetailsHandler viewDetailsHandler,
        ActivateConnectorHandler activateHandler)
    {
        _createHandler = createHandler;
        _browseHandler = browseHandler;
        _viewDetailsHandler = viewDetailsHandler;
        _activateHandler = activateHandler;
    }

    /// <summary>
    /// Creates a new connector.
    /// </summary>
    /// <param name="request">Create request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new resource id, or 409 Conflict if duplicate.</returns>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateConnectorRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateConnectorCommand(
            request.Name,
            request.Description,
            request.ExternalSystemId,
            request.BaseUrl,
            request.Protocol,
            request.AuthenticationType,
            request.TimeoutSeconds);

        try
        {
            var id = await _createHandler.Handle(command, cancellationToken);
            return Created($"/api/connectors/{id}", new { id });
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
    /// Browse connectors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with a list of connector summaries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ConnectorsResponseModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> Browse(CancellationToken cancellationToken)
    {
        var query = new BrowseConnectorsQuery();
        var response = await _browseHandler.Handle(query, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// View connector detail by id.
    /// </summary>
    /// <param name="id">Connector id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with details, or 404 NotFound if the id does not exist.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ConnectorSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewDetails([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var query = new ViewConnectorDetailsQuery(id);
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

    /// <summary>
    /// Activates a draft connector.
    /// </summary>
    /// <param name="id">Connector id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content, 404 Not Found, or 409 Conflict.</returns>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new ActivateConnectorCommand(id);

        try
        {
            await _activateHandler.Handle(command, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    /// <summary>
    /// Retires a active connector.
    /// </summary>
    /// <param name="id">Connector id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content, 404 Not Found, or 409 Conflict.</returns>
    [HttpPost("{id:guid}/retire")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]    
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retire([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new RetireConnectorCommand(id);

        try
        {
            var retireHandler = HttpContext.RequestServices.GetRequiredService<RetireConnectorHandler>();
            await retireHandler.Handle(command, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }


}
