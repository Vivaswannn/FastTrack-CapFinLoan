using CapFinLoan.AdminService.Services.Interfaces;

namespace CapFinLoan.AdminService.Services
{
    /// <summary>
    /// HTTP client implementation that calls ApplicationService endpoints.
    /// Failures are caught and logged — they never propagate to the caller.
    /// </summary>
    public class ApplicationHttpService : IApplicationHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ApplicationHttpService> _logger;

        public ApplicationHttpService(
            IHttpClientFactory httpClientFactory,
            ILogger<ApplicationHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task UpdateApplicationStatusAsync(
            Guid applicationId,
            string newStatus,
            string remarks,
            string adminToken)
        {
            _logger.LogInformation(
                "Calling ApplicationService to update status to {Status} for app {AppId}",
                newStatus, applicationId);

            try
            {
                var client = _httpClientFactory.CreateClient("ApplicationService");

                var payload = new { newStatus, remarks };
                var json    = System.Text.Json.JsonSerializer.Serialize(payload);

                // Use HttpRequestMessage so the Authorization header travels
                // correctly through the Polly resilience pipeline on retries
                var request = new HttpRequestMessage(
                    HttpMethod.Put,
                    $"/api/admin/applications/{applicationId}/status");
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", adminToken);
                request.Content = new StringContent(
                    json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                _logger.LogInformation(
                    "ApplicationService status update response: {StatusCode} for app {AppId}",
                    response.StatusCode, applicationId);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Status update failed for application {ApplicationId}: " +
                        "HTTP {Status} — {Body}",
                        applicationId, response.StatusCode, body);
                }
                else
                {
                    _logger.LogInformation(
                        "Application {ApplicationId} status updated to {Status} successfully",
                        applicationId, newStatus);
                }
            }
            catch (Exception ex)
            {
                // Decision is already saved — log the failure but do not roll back
                _logger.LogError(ex,
                    "Failed to update application status for {ApplicationId}",
                    applicationId);
            }
        }

        /// <inheritdoc/>
        public async Task<string?> GetApplicationStatusAsync(
            Guid applicationId,
            string adminToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApplicationService");
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/api/applications/{applicationId}");
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", adminToken);
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                var body = await response.Content.ReadAsStringAsync();
                // Extract status from: {"success":true,"data":{"status":"UnderReview",...}}
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                return doc.RootElement
                    .GetProperty("data")
                    .GetProperty("status")
                    .GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get application status for {ApplicationId}", applicationId);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<string?> GetApplicationQueueAsync(
            int page,
            int pageSize,
            string? statusFilter,
            string adminToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApplicationService");

                var url = $"/api/admin/applications?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(statusFilter))
                    url += $"&status={statusFilter}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", adminToken);

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "GetApplicationQueue failed: HTTP {Status}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve application queue");
                return null;
            }
        }
    }
}
