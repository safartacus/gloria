using System.Diagnostics;
using Gloria.Application.Auth;
using Gloria.Domain.Common;
using Serilog.Context;

namespace Gloria.Api.Middleware;

/// <summary>
/// Single place unhandled failures are turned into a clean problem response. Every caught exception
/// is logged through Serilog (Console + Seq) with a correlation id, the request path/method and the
/// caller, and that same id is returned to the client so a report can be traced in Seq.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = Activity.Current?.Id ?? context.TraceIdentifier;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("User", context.User.Identity?.Name ?? "anonymous"))
        {
            try
            {
                await _next(context);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogWarning(ex, "Authentication failure");
                await WriteProblem(context, StatusCodes.Status401Unauthorized, "Kimlik doğrulama hatası", ex.Message, correlationId);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation");
                await WriteProblem(context, StatusCodes.Status409Conflict, "Business rule violation", ex.Message, correlationId);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Referenced file missing");
                await WriteProblem(context, StatusCodes.Status400BadRequest, "File not found", ex.Message, correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteProblem(context, StatusCodes.Status500InternalServerError, "Unexpected error", ex.Message, correlationId);
            }
        }
    }

    private static async Task WriteProblem(HttpContext context, int status, string title, string detail, string correlationId)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetailsBody(title, detail, status, correlationId));
    }

    private sealed record ProblemDetailsBody(string Title, string Detail, int Status, string CorrelationId);
}
