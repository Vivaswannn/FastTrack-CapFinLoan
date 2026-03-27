using CapFinLoan.ApplicationService.Models;
using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.ApplicationService.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for loan application data access operations.
    /// Only the implementation class may use DbContext directly.
    /// </summary>
    public interface ILoanApplicationRepository
    {
        /// <summary>
        /// Retrieves a loan application by its ID, including full status history.
        /// </summary>
        /// <param name="applicationId">The application GUID.</param>
        /// <returns>The application with StatusHistories loaded, or null if not found.</returns>
        Task<LoanApplication?> GetByIdAsync(Guid applicationId);

        /// <summary>
        /// Retrieves a loan application by ID only if it belongs to the specified user.
        /// Used for applicant access control — prevents cross-user data access.
        /// </summary>
        /// <param name="applicationId">The application GUID.</param>
        /// <param name="userId">The owner's user GUID.</param>
        /// <returns>The application if found and owned by the user, otherwise null.</returns>
        Task<LoanApplication?> GetByIdAndUserIdAsync(Guid applicationId, Guid userId);

        /// <summary>
        /// Returns a paginated list of all applications belonging to a specific user.
        /// StatusHistories are NOT included for performance (list view).
        /// </summary>
        /// <param name="userId">The user GUID to filter by.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>Tuple of (applications for the page, total count).</returns>
        Task<(List<LoanApplication> Applications, int TotalCount)> GetByUserIdAsync(
            Guid userId, int page, int pageSize);

        /// <summary>
        /// Returns a paginated list of all applications (admin view) with optional status filter.
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="statusFilter">Optional status to filter by.</param>
        /// <returns>Tuple of (applications for the page, total count).</returns>
        Task<(List<LoanApplication> Applications, int TotalCount)> GetAllAsync(
            int page, int pageSize, ApplicationStatus? statusFilter = null);

        /// <summary>
        /// Persists a new loan application to the database.
        /// </summary>
        /// <param name="application">The application to create.</param>
        /// <returns>The created application with any database-generated values.</returns>
        Task<LoanApplication> CreateAsync(LoanApplication application);

        /// <summary>
        /// Persists changes to an existing loan application.
        /// </summary>
        /// <param name="application">The modified application.</param>
        /// <returns>The updated application.</returns>
        Task<LoanApplication> UpdateAsync(LoanApplication application);

        /// <summary>
        /// Adds a new status history record to the audit trail.
        /// Called on every status transition.
        /// </summary>
        /// <param name="history">The history record to add.</param>
        Task AddStatusHistoryAsync(StatusHistory history);

        /// <summary>
        /// Returns the complete status history for an application, ordered chronologically.
        /// </summary>
        /// <param name="applicationId">The application GUID.</param>
        /// <returns>List of history records ordered by ChangedAt ascending.</returns>
        Task<List<StatusHistory>> GetStatusHistoryAsync(Guid applicationId);

        /// <summary>
        /// Returns the count of applications in a given status.
        /// Used for dashboard statistics.
        /// </summary>
        /// <param name="status">The status to count.</param>
        /// <returns>Count of applications in that status.</returns>
        Task<int> GetTotalCountByStatusAsync(ApplicationStatus status);

        /// <summary>
        /// Saves an outbox message to the database within the current transaction context.
        /// </summary>
        /// <param name="message">The serialized outbox message.</param>
        Task SaveOutboxMessageAsync(OutboxMessage message);
    }
}
