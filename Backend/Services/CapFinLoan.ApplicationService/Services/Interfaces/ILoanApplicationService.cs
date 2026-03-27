using CapFinLoan.ApplicationService.DTOs.Requests;
using CapFinLoan.ApplicationService.DTOs.Responses;
using CapFinLoan.SharedKernel.DTOs;
using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.ApplicationService.Services.Interfaces
{
    /// <summary>
    /// Service interface for loan application business logic.
    /// All business rules are enforced here — never in controllers or repositories.
    /// </summary>
    public interface ILoanApplicationService
    {
        /// <summary>
        /// Creates a new loan application with Status=Draft.
        /// </summary>
        /// <param name="userId">The authenticated applicant's user GUID (from JWT).</param>
        /// <param name="dto">The creation request data.</param>
        /// <returns>The created application as a response DTO.</returns>
        Task<LoanApplicationResponseDto> CreateDraftAsync(Guid userId, CreateLoanApplicationDto dto);

        /// <summary>
        /// Updates an existing draft application.
        /// Only the owner can update; only Draft status applications can be modified.
        /// </summary>
        /// <param name="applicationId">The application GUID.</param>
        /// <param name="userId">The authenticated applicant's user GUID (from JWT).</param>
        /// <param name="dto">Fields to update (null fields are ignored).</param>
        /// <returns>The updated application as a response DTO.</returns>
        Task<LoanApplicationResponseDto> UpdateDraftAsync(
            Guid applicationId, Guid userId, UpdateLoanApplicationDto dto);

        /// <summary>
        /// Submits a draft application, transitioning it from Draft to Submitted.
        /// Validates that required employment fields are present before allowing submission.
        /// </summary>
        /// <param name="applicationId">The application GUID.</param>
        /// <param name="userId">The authenticated applicant's user GUID (from JWT).</param>
        /// <param name="userEmail">The authenticated applicant's email (from JWT).</param>
        /// <returns>The submitted application as a response DTO.</returns>
        Task<LoanApplicationResponseDto> SubmitAsync(
            Guid applicationId, Guid userId, string userEmail);

        /// <summary>
        /// Gets a single application by ID.
        /// Admin can view any application; Applicant can only view their own.
        /// </summary>
        /// <param name="applicationId">The application GUID.</param>
        /// <param name="userId">The requesting user's GUID (from JWT).</param>
        /// <param name="role">The requesting user's role (from JWT).</param>
        /// <returns>The application with full status history as a response DTO.</returns>
        Task<LoanApplicationResponseDto> GetByIdAsync(
            Guid applicationId, Guid userId, string role);

        /// <summary>
        /// Returns a paginated list of applications belonging to the authenticated applicant.
        /// </summary>
        /// <param name="userId">The applicant's user GUID (from JWT).</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <returns>Paginated response of application DTOs.</returns>
        Task<PagedResponseDto<LoanApplicationResponseDto>> GetMyApplicationsAsync(
            Guid userId, int page, int pageSize);

        /// <summary>
        /// Returns a paginated list of all applications for admin view, with optional status filter.
        /// </summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="statusFilter">Optional status to filter by.</param>
        /// <returns>Paginated response of application DTOs.</returns>
        Task<PagedResponseDto<LoanApplicationResponseDto>> GetAllApplicationsAsync(
            int page, int pageSize, ApplicationStatus? statusFilter);

        /// <summary>
        /// Admin updates the status of an application, enforcing the strict status machine.
        /// Creates a StatusHistory record for every transition.
        /// </summary>
        /// <param name="applicationId">The application GUID.</param>
        /// <param name="dto">The new status and optional remarks.</param>
        /// <param name="adminEmail">The admin's email (from JWT).</param>
        /// <returns>The updated application as a response DTO.</returns>
        Task<LoanApplicationResponseDto> UpdateStatusAsync(
            Guid applicationId, UpdateApplicationStatusDto dto, string adminEmail);

        /// <summary>
        /// Returns the full status audit trail for an application.
        /// Applicant can only view history for their own applications.
        /// </summary>
        /// <param name="applicationId">The application GUID.</param>
        /// <param name="userId">The requesting user's GUID (from JWT).</param>
        /// <param name="role">The requesting user's role (from JWT).</param>
        /// <returns>List of status history entries ordered chronologically.</returns>
        Task<List<StatusHistoryResponseDto>> GetStatusHistoryAsync(
            Guid applicationId, Guid userId, string role);
    }
}
