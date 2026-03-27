using System.Net;
using System.Text.Json;
using FluentValidation;

namespace CapFinLoan.AdminService.Exceptions
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
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
            catch (ValidationException ex)
            {
                _logger.LogWarning(
                    "Validation error: {Message}", ex.Message);
                await WriteErrorResponse(context,
                    HttpStatusCode.BadRequest,
                    string.Join(", ",
                        ex.Errors.Select(e => e.ErrorMessage)));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(
                    "Not found: {Message}", ex.Message);
                await WriteErrorResponse(context,
                    HttpStatusCode.NotFound, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(
                    "Unauthorized: {Message}", ex.Message);
                await WriteErrorResponse(context,
                    HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    "Bad request: {Message}", ex.Message);
                await WriteErrorResponse(context,
                    HttpStatusCode.BadRequest, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "Invalid operation: {Message}", ex.Message);
                await WriteErrorResponse(context,
                    HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception: {Message}", ex.Message);
                await WriteErrorResponse(context,
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred. " +
                    "Please try again.");
            }
        }

        private static async Task WriteErrorResponse(
            HttpContext context,
            HttpStatusCode statusCode,
            string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                success = false,
                message = message,
                data = (object?)null,
                errors = new[] { message }
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy =
                            JsonNamingPolicy.CamelCase
                    }));
        }
    }
}
