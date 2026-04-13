namespace CapFinLoan.AdminService.Services.Interfaces
{
    /// <summary>
    /// HTTP client abstraction for communicating with ApplicationService.
    /// AdminService does not own application data — it proxies requests.
    /// </summary>
    public interface IApplicationHttpService
    {
        /// <summary>
        /// Updates application status in ApplicationService after a decision is made.
        /// Called fire-and-forget style — failure is logged but does not abort the decision.
        /// </summary>
        Task UpdateApplicationStatusAsync(
            Guid applicationId,
            string newStatus,
            string remarks,
            string adminToken);

        /// <summary>
        /// Fetches the paginated application queue from ApplicationService.
        /// Returns raw JSON string; AdminService does not own this data.
        /// Returns null on failure.
        /// </summary>
        Task<string?> GetApplicationQueueAsync(
            int page,
            int pageSize,
            string? statusFilter,
            string adminToken);

        /// <summary>
        /// Fetches a single application's current status from ApplicationService.
        /// Returns null if the application is not found or the call fails.
        /// </summary>
        Task<string?> GetApplicationStatusAsync(
            Guid applicationId,
            string adminToken);
    }
}
