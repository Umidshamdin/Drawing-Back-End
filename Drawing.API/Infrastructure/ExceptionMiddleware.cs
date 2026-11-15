using Drawing.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Drawing.API.Infrastructure;
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            if (context.Response.HasStarted) throw;

            var status = ex switch
            {
                KeyNotFoundException => 404,
                UnauthorizedAccessException => 403,
                DomainException => 409,
                ArgumentException => 400,
                DbUpdateConcurrencyException => 409,

                PostgresException pg when pg.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation => 409,

                DbUpdateException => 409,
                InvalidOperationException => 409,
                _ => 500
            };

            if (status == 500) _logger.LogError(ex, "Unhandled API exception");
            else _logger.LogWarning("Rejected API request: {Reason}", ex.Message);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = status,
                Title = status == 500 ? "Server error" : "Request error",
                Detail = status == 500 ? "Daxili server xətası." : ex.Message,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
