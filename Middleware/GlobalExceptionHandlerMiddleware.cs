using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace GiaLaiOCOP.Api.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
                _logger.LogError(ex, "An unhandled exception occurred. Request: {Method} {Path}", 
                    context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError; // 500 if unexpected
            var result = string.Empty;
            var isDevelopment = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

            switch (exception)
            {
                case UnauthorizedAccessException:
                    code = HttpStatusCode.Unauthorized;
                    result = JsonSerializer.Serialize(new { error = "Unauthorized", message = exception.Message });
                    break;
                case ArgumentNullException:
                    code = HttpStatusCode.BadRequest;
                    result = JsonSerializer.Serialize(new { error = "Bad Request", message = exception.Message });
                    break;
                case ArgumentException:
                    code = HttpStatusCode.BadRequest;
                    result = JsonSerializer.Serialize(new { error = "Bad Request", message = exception.Message });
                    break;
                case KeyNotFoundException:
                case FileNotFoundException:
                    code = HttpStatusCode.NotFound;
                    result = JsonSerializer.Serialize(new { error = "Not Found", message = exception.Message });
                    break;
                default:
                    // Log full exception details for internal server errors
                    _logger.LogError(exception, "Unhandled exception: {ExceptionType} - {Message}\n{StackTrace}", 
                        exception.GetType().Name, exception.Message, exception.StackTrace);
                    
                    // In development, return more details. In production, return generic message
                    if (isDevelopment)
                    {
                        result = JsonSerializer.Serialize(new 
                        { 
                            error = "Internal Server Error", 
                            message = exception.Message,
                            type = exception.GetType().Name,
                            stackTrace = exception.StackTrace,
                            innerException = exception.InnerException != null ? new
                            {
                                message = exception.InnerException.Message,
                                type = exception.InnerException.GetType().Name
                            } : null
                        });
                    }
                    else
                    {
                        result = JsonSerializer.Serialize(new 
                        { 
                            error = "Internal Server Error", 
                            message = "An error occurred while processing your request.",
                            details = exception.Message // Include message but not stack trace in production
                        });
                    }
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            return context.Response.WriteAsync(result);
        }
    }
}

