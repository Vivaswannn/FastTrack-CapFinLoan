using CapFinLoan.AdminService.DTOs.Requests;
using CapFinLoan.AdminService.DTOs.Responses;

namespace CapFinLoan.AdminService.Services.Interfaces
{
    /// <summary>
    /// Business-logic interface for admin decisions and reporting.
    /// </summary>
    public interface IAdminService
    {
        /// <summary>
        /// Records an approve or reject decision for a loan application
        /// and triggers a status update in ApplicationService.
        /// </summary>
        Task<DecisionResponseDto> MakeDecisionAsync(
            Guid applicationId,
            MakeDecisionDto dto,
            Guid adminId,
            string adminEmail,
            string adminToken);

        /// <summary>
        /// Returns the decision for a given application, or null if none exists yet.
        /// Accessible by both admin and the applicant.
        /// </summary>
        Task<DecisionResponseDto?> GetDecisionByApplicationAsync(Guid applicationId);

        /// <summary>Returns aggregated KPI stats for the admin dashboard.</summary>
        Task<DashboardStatsDto> GetDashboardStatsAsync();

        /// <summary>Returns decisions grouped by month for the trend chart.</summary>
        Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months);

        /// <summary>
        /// Proxies the application queue from ApplicationService.
        /// Returns raw JSON string for the React frontend to consume.
        /// </summary>
        Task<string> GetApplicationQueueAsync(
            int page,
            int pageSize,
            string? statusFilter,
            string adminToken);

        /// <summary>
        /// Exports decisions to a UTF-8 encoded CSV byte array.
        /// Optionally filtered by date range.
        /// </summary>
        Task<byte[]> ExportDecisionsToCsvAsync(
            DateTime? startDate,
            DateTime? endDate);
    }
}
