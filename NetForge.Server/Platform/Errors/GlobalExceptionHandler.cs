using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NetForge.Server.Platform.Errors;

/// <summary>
/// Translates every unhandled exception into an RFC 7807 ProblemDetails response.
/// Registered via AddExceptionHandler + UseExceptionHandler in Program.cs.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var error = Map(exception);

        if (error.Status >= 500)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning("Handled {Code}: {Message}", error.Code, exception.Message);

        httpContext.Response.StatusCode = error.Status;

        var problem = new ProblemDetails
        {
            Status = error.Status,
            Title = error.Title,
            Detail = error.Detail,
            Type = $"https://docs.netforge.dev/errors/{error.Code.ToLowerInvariant().Replace('_', '-')}",
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        if (error.Errors is not null)
            problem.Extensions["errors"] = error.Errors;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    private static ErrorInfo Map(Exception exception) => exception switch
    {
        DomainException d => new(d.Status, d.Code, ReasonPhrase(d.Status), d.Message, d.Errors),
        // Optimistic concurrency (§6.22): surface a standard 409 the FE can recover from.
        DbUpdateConcurrencyException => new(StatusCodes.Status409Conflict, "CONCURRENCY_CONFLICT", "Conflict",
            "This record was changed by someone else. Refresh and try again.", null),
        _ => new(StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Internal Server Error",
            "An unexpected error occurred.", null),
    };

    private static string ReasonPhrase(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Error",
    };

    private readonly record struct ErrorInfo(
        int Status, string Code, string Title, string Detail, IReadOnlyDictionary<string, string[]>? Errors);
}
