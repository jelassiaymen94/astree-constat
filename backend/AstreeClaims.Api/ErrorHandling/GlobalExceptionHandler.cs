using System.Data.Common;
using System.Diagnostics;
using AstreeClaims.Api.DTOs.Common;
using AstreeClaims.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace AstreeClaims.Api.ErrorHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var error = MapException(exception, traceId);

        if (error.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Request failed. TraceId={TraceId}, Method={Method}, Path={Path}, Code={Code}",
                traceId,
                httpContext.Request.Method,
                httpContext.Request.Path,
                error.Response.Code);
        }
        else
        {
            _logger.LogWarning(
                "Request rejected. TraceId={TraceId}, Method={Method}, Path={Path}, Code={Code}",
                traceId,
                httpContext.Request.Method,
                httpContext.Request.Path,
                error.Response.Code);
        }

        httpContext.Response.StatusCode = error.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            error.Response,
            cancellationToken);

        return true;
    }

    private static MappedError MapException(Exception exception, string traceId) =>
        exception switch
        {
            ApiException apiException => new MappedError(
                apiException.StatusCode,
                new ApiErrorDto(
                    apiException.Code,
                    apiException.Message,
                    traceId)),

            DbException => new MappedError(
                StatusCodes.Status503ServiceUnavailable,
                new ApiErrorDto(
                    "DATABASE_UNAVAILABLE",
                    "Le service de données est temporairement indisponible.",
                    traceId)),

            _ => new MappedError(
                StatusCodes.Status500InternalServerError,
                new ApiErrorDto(
                    "INTERNAL_ERROR",
                    "Une erreur interne est survenue.",
                    traceId))
        };

    private sealed record MappedError(int StatusCode, ApiErrorDto Response);
}
