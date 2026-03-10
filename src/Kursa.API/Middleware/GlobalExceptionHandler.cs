using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails = exception switch
        {
            ValidationException validationException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Validation Error",
                Detail = string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage)),
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Forbidden",
                Detail = "You do not have permission to access this resource.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4"
            },
            KeyNotFoundException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Not Found",
                Detail = exception.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
            },
            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            }
        };

        if (problemDetails.Status == (int)HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning(exception, "Handled exception: {ExceptionType} - {Message}",
                exception.GetType().Name, exception.Message);
        }

        string? correlationId = httpContext.Items["CorrelationId"]?.ToString();
        if (correlationId is not null)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
