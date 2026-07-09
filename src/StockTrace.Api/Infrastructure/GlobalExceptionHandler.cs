using StockTrace.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StockTrace.Api.Infrastructure;

internal sealed partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException =>
                (StatusCodes.Status400BadRequest, "Validation failed.", validationException.Errors),
            NotFoundException =>
                (StatusCodes.Status404NotFound, exception.Message, null),
            ConflictException =>
                (StatusCodes.Status409Conflict, exception.Message, null),
            _ =>
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError) LogUnhandledException(exception);
        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = errors is null ? new ProblemDetails
            {
                Status = statusCode,
                Title = title
            } : new HttpValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title = title
            }
        });
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred while processing the request.")]
    private partial void LogUnhandledException(Exception exception);
}
