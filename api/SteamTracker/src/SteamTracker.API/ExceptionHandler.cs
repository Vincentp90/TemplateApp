using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SteamTracker.Domain.Exceptions;
using SteamTracker.Infrastructure.External;

namespace SteamTracker.API;

/// <summary>
/// Global exception handler that returns a 500 status code with ProblemDetails.
/// </summary>
public class ExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ExceptionHandler> _logger;

    public ExceptionHandler(ILogger<ExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title, type) = exception switch
        {
            TrackingNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "https://tools.ietf.org/html/rfc7231#section-6.5.4"),
            AlertRuleNotFoundException => (StatusCodes.Status404NotFound, "Not Found", "https://tools.ietf.org/html/rfc7231#section-6.5.4"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", "https://tools.ietf.org/html/rfc7231#section-6.5.1"),
            SteamRateLimitException => (StatusCodes.Status429TooManyRequests, "Rate Limited", "https://tools.ietf.org/html/rfc6585#section-4"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "https://tools.ietf.org/html/rfc7231#section-6.6.1")
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
