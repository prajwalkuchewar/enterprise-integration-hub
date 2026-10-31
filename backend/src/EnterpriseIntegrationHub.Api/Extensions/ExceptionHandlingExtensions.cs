using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseIntegrationHub.Api.Extensions;

internal static class ExceptionHandlingExtensions
{
    public static void UseApiExceptionHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(exceptionApp =>
        {
            exceptionApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var problem = exception switch
                {
                    ArgumentException => CreateProblem(StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
                    KeyNotFoundException => CreateProblem(StatusCodes.Status404NotFound, "Resource not found", exception.Message),
                    InvalidOperationException => CreateProblem(StatusCodes.Status409Conflict, "Operation conflicts with the current state", exception.Message),
                    _ => CreateProblem(StatusCodes.Status500InternalServerError, "An unexpected error occurred", "An unexpected error occurred.")
                };

                context.Response.StatusCode = problem.Status!.Value;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}"
        };
}

