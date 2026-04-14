using Serilog.Context;

namespace CapFinLoan.DocumentService.Exceptions
{
    /// <summary>
    /// Reads or generates a Correlation ID for every request,
    /// echoes it in the response header, and enriches all
    /// Serilog log entries with the value.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.Response.Headers[CorrelationIdHeader] = correlationId;
            context.Items[CorrelationIdHeader] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}
