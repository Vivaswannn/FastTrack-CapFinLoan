using CapFinLoan.ApplicationService.DTOs.Requests;
using CapFinLoan.ApplicationService.DTOs.Responses;
using CapFinLoan.ApplicationService.Services.Interfaces;
using CapFinLoan.SharedKernel.DTOs;
using CapFinLoan.SharedKernel.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CapFinLoan.ApplicationService.Controllers
{
    /// <summary>
    /// Admin-only endpoints for viewing and updating the loan application queue.
    /// All endpoints require Admin role.
    /// </summary>
    [ApiController]
    [Route("api/admin/applications")]
    [Authorize(Roles = "Admin")]
    public class AdminLoanController : ControllerBase
    {
        private readonly ILoanApplicationService _service;
        private readonly ILogger<AdminLoanController> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="AdminLoanController"/>.
        /// </summary>
        public AdminLoanController(
            ILoanApplicationService service,
            ILogger<AdminLoanController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Returns all loan applications paginated, with optional status filter.
        /// Accepts <see cref="PaginationFilter"/> for page/pageSize binding.
        /// </summary>
        /// <param name="filter">Pagination parameters (pageNumber, pageSize).</param>
        /// <param name="status">Optional status filter.</param>
        /// <returns>Paginated list of all applications.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllApplications(
            [FromQuery] PaginationFilter filter,
            [FromQuery] ApplicationStatus? status = null)
        {
            PagedResponseDto<LoanApplicationResponseDto> result =
                await _service.GetAllApplicationsAsync(filter.PageNumber, filter.PageSize, status);
            return Ok(ApiResponseDto<PagedResponseDto<LoanApplicationResponseDto>>.SuccessResponse(
                result, "Applications retrieved successfully."));
        }

        /// <summary>
        /// Updates the status of a loan application.
        /// Enforces strict status machine — invalid transitions return 400.
        /// </summary>
        /// <param name="id">The application GUID.</param>
        /// <param name="dto">The new status and optional remarks.</param>
        /// <returns>The updated application.</returns>
        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateApplicationStatusDto dto)
        {
            string adminEmail = GetEmailFromClaims();
            LoanApplicationResponseDto result = await _service.UpdateStatusAsync(id, dto, adminEmail);
            return Ok(ApiResponseDto<LoanApplicationResponseDto>.SuccessResponse(
                result, "Application status updated successfully."));
        }

        /// <summary>Extracts the authenticated admin's email from JWT claims.</summary>
        private string GetEmailFromClaims()
        {
            string email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email")
                ?? string.Empty;
            return email;
        }
    }
}
