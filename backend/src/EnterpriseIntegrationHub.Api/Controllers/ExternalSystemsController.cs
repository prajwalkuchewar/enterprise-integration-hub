using EnterpriseIntegrationHub.Api.Contracts.Requests;
using EnterpriseIntegrationHub.Application.Features.ExternalSystems.Create;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseIntegrationHub.Api.Controllers;

[ApiController]
[Route("api/external-systems")]
public sealed class ExternalSystemsController : ControllerBase
{
    private readonly CreateExternalSystemHandler _handler;
    private readonly BrowseExternalSystemHandler _browseHandler;


    public ExternalSystemsController(CreateExternalSystemHandler handler, BrowseExternalSystemHandler browseHandler)
    {
        _handler = handler;
        _browseHandler = browseHandler;
    }

    [HttpPost]
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


    [HttpGet]
    public async Task<IActionResult> Browse(CancellationToken cancellationToken)
    {
        var response = await _browseHandler.Handle(cancellationToken);

        return Ok(response);
    }
}
