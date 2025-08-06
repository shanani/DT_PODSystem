using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Integration
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the exception
            LogException(context, exception);

            // Set response status code and content type
            context.Response.StatusCode = GetStatusCode(exception);
            context.Response.ContentType = "application/json";

            // Create error response
            var response = CreateErrorResponse(context, exception);

            // Write response
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private void LogException(HttpContext context, Exception exception)
        {
            var userId = context.User?.Identity?.Name ?? "Anonymous";
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            _logger.LogError(exception,
                "Unhandled exception occurred. User: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, IP: {IPAddress}, UserAgent: {UserAgent}",
                userId, requestPath, requestMethod, ipAddress, userAgent);
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => (int)HttpStatusCode.BadRequest,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                NotImplementedException => (int)HttpStatusCode.NotImplemented,
                TimeoutException => (int)HttpStatusCode.RequestTimeout,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }

        private object CreateErrorResponse(HttpContext context, Exception exception)
        {
            var statusCode = GetStatusCode(exception);
            var response = new
            {
                error = new
                {
                    message = GetErrorMessage(exception),
                    statusCode,
                    timestamp = DateTime.UtcNow.AddHours(3),
                    path = context.Request.Path.Value,
                    method = context.Request.Method
                }
            };

            // Add detailed error information only in development
            if (_environment.IsDevelopment())
            {
                return new
                {
                    error = new
                    {
                        message = exception.Message,
                        statusCode,
                        timestamp = DateTime.UtcNow.AddHours(3),
                        path = context.Request.Path.Value,
                        method = context.Request.Method,
                        stackTrace = exception.StackTrace,
                        type = exception.GetType().Name,
                        innerException = exception.InnerException?.Message
                    }
                };
            }

            return response;
        }

        private static string GetErrorMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => "Invalid request: Missing required parameter.",
                ArgumentException => "Invalid request: Invalid parameter value.",
                UnauthorizedAccessException => "Access denied: You don't have permission to access this resource.",
                NotImplementedException => "Feature not implemented.",
                TimeoutException => "Request timeout: The operation took too long to complete.",
                _ => "An unexpected error occurred. Please try again later."
            };
        }
    }
}