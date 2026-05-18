using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WifiProvisioning.Core.Exceptions;
using static WifiProvisioning.Core.Exceptions.ProvisioningException;

namespace WifiProvisioning.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions from downstream middleware/controllers and
/// converts them into RFC 7807 ProblemDetails responses with the correct
/// HTTP status code.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = MapException(exception);

        if ((int)statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Title}", title);
        }
        else
        {
            _logger.LogWarning(exception, "Handled domain exception: {Title}", title);
        }

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = _environment.IsDevelopment() ? exception.Message : "See server logs for details.",
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problem);
        await context.Response.WriteAsync(json).ConfigureAwait(false);
    }

    private static (HttpStatusCode StatusCode, string Title) MapException(Exception exception) =>
     exception switch
     {
         SpeedNotAvailableException =>
             (HttpStatusCode.NotFound, "Requested speed not available"),

         InvalidProvisioningOrderException =>
             (HttpStatusCode.BadRequest, "Invalid provisioning order"),

         SpeedProfileServiceException sp when IsTimeout(sp) =>
             (HttpStatusCode.GatewayTimeout, "Upstream Network Infrastructure API timed out"),

         SpeedProfileServiceException =>
             (HttpStatusCode.BadGateway, "Upstream Network Infrastructure API failure"),

         ActivationServiceException a when IsTimeout(a) =>
             (HttpStatusCode.GatewayTimeout, "Upstream Network Controller API timed out"),

         ActivationServiceException =>
             (HttpStatusCode.BadGateway, "Upstream Network Controller API failure"),

         OperationCanceledException =>
             (HttpStatusCode.RequestTimeout, "Request was cancelled"),

         _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
     };

    private static bool IsTimeout(Exception ex) =>
        ex.InnerException is TaskCanceledException
        || ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase);
}