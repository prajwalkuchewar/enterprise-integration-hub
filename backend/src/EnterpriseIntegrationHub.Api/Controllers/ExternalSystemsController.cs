using EnterpriseIntegrationHub.Api.Contracts.Requests;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Browse;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Create;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseIntegrationHub.Api.Controllers;

/// <summary>
/// Controller for managing external systems.
/// </summary>
[ApiController]
[Route("api/external-systems")]
public sealed class ExternalSystemsController : ControllerBase
{
    private readonly CreateExternalSystemHandler _handler;
    private readonly BrowseExternalSystemHandler _browseHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalSystemsController"/> class.
    /// </summary>
    public ExternalSystemsController(CreateExternalSystemHandler handler, BrowseExternalSystemHandler browseHandler)
    {
        _handler = handler;
        _browseHandler = browseHandler;
    }

    /// <summary>
    /// Creates a new external system.
    /// </summary>
    /// <param name="request">Create request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [Consumes("application/json")]
    public async Task<IActionResult> Create(
        CreateExternalSystemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateExternalSystemCommand(
            request.Name,
            request.Description,
            request.Environment);

        try
        {
            var id = await _handler.Handle(command, cancellationToken);

            return Created($"/api/external-systems/{id}", new { id });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }


    /// <summary>
    /// Browse external systems. Query parameters are bound from the query string.
    /// </summary>
    /// <param name="request">Browse request bound from query string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> Browse([FromQuery] BrowseExternalSystemRequest request,
CancellationToken cancellationToken)
    {
        // Convert request -> query if needed. Example kept minimal:
        var query = new BrowseExternalSystemQuery();

        var response = await _browseHandler.Handle(query, cancellationToken);

        return Ok(response);
    }
}
