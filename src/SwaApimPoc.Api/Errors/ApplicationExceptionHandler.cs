using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SwaApimPoc.Application.Common;

namespace SwaApimPoc.Api.Errors;

public sealed class ApplicationExceptionHandler(ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int status = exception switch
        {
            UnauthenticatedException => StatusCodes.Status401Unauthorized,
            GraphTokenMissingException => StatusCodes.Status400BadRequest,
            IdentityMismatchException => StatusCodes.Status403Forbidden,
            GraphCallFailedException => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError,
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception.");
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = status == StatusCodes.Status500InternalServerError ? "Unexpected error." : exception.Message,
            },
            cancellationToken);
        return true;
    }
}
