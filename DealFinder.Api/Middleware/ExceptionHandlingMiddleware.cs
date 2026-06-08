using DealFinder.Api.Exceptions;
using DealFinder.Api.Models;
using System.Net;
using System.Text.Json;

namespace DealFinder.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteErrorResponseAsync(
                context,
                ex.StatusCode,
                ex.Message,
                ex.Code
            );
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timed out or was cancelled.");

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status408RequestTimeout,
                "The request timed out. Please try again.",
                "REQUEST_TIMEOUT"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled server error.");

            await WriteErrorResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Something went wrong. Please try again later.",
                "SERVER_ERROR"
            );
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        string message,
        string code
    )
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse
        {
            Message = message,
            Code = code,
            TraceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}