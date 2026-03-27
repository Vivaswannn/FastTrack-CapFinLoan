using CapFinLoan.ApplicationService.DTOs.Requests;
using CapFinLoan.ApplicationService.DTOs.Responses;
using CapFinLoan.ApplicationService.Services.Interfaces;
using CapFinLoan.SharedKernel.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CapFinLoan.ApplicationService.Controllers
{
    /// <summary>
    /// Handles loan application endpoints for authenticated applicants (and admins).
    /// All endpoints require a valid JWT token.
    /// </summary>
    [ApiController]
    [Route("api/applications")]
    [Authorize]
    public class LoanApplicationController : ControllerBase
    {
        private readonly ILoanApplicationService _service;
        private readonly ILogger<LoanApplicationController> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="LoanApplicationController"/>.
        /// </summary>
        public LoanApplicationController(
            ILoanApplicationService service,
            ILogger<LoanApplicationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new loan application in Draft status.
        /// </summary>
        /// <param name="dto">The loan application data.</param>
        /// <returns>The created application.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateDraft([FromBody] CreateLoanApplicationDto dto)
        {
            Guid userId = GetUserIdFromClaims();
            LoanApplicationResponseDto result = await _service.CreateDraftAsync(userId, dto);
            return StatusCode(201,
                ApiResponseDto<LoanApplicationResponseDto>.SuccessResponse(
                    result, "Loan application draft created successfully."));
        }

        /// <summary>
        /// Updates an existing draft loan application.
        /// Only the owner can update; only Draft status applications can be modified.
        /// </summary>
        /// <param name="id">The application GUID.</param>
        /// <param name="dto">Fields to update.</param>
        /// <returns>The updated application.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] UpdateLoanApplicationDto dto)
        {
            Guid userId = GetUserIdFromClaims();
            LoanApplicationResponseDto result = await _service.UpdateDraftAsync(id, userId, dto);
            return Ok(ApiResponseDto<LoanApplicationResponseDto>.SuccessResponse(
                result, "Loan application updated successfully."));
        }

        /// <summary>
        /// Submits a draft application, transitioning it from Draft to Submitted.
        /// </summary>
        /// <param name="id">The application GUID.</param>
        /// <returns>The submitted application.</returns>
        [HttpPost("{id:guid}/submit")]
        public async Task<IActionResult> Submit(Guid id)
        {
            Guid userId = GetUserIdFromClaims();
            string email = GetEmailFromClaims();
            LoanApplicationResponseDto result = await _service.SubmitAsync(id, userId, email);
            return Ok(ApiResponseDto<LoanApplicationResponseDto>.SuccessResponse(
                result, "Application submitted successfully."));
        }

        /// <summary>
        /// Returns a paginated list of the authenticated applicant's own applications.
        /// Accepts <see cref="PaginationFilter"/> for page/pageSize binding.
        /// </summary>
        /// <param name="filter">Pagination parameters (pageNumber, pageSize).</param>
        /// <returns>Paginated list of applications.</returns>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyApplications(
            [FromQuery] PaginationFilter filter)
        {
            Guid userId = GetUserIdFromClaims();
            PagedResponseDto<LoanApplicationResponseDto> result =
                await _service.GetMyApplicationsAsync(userId, filter.PageNumber, filter.PageSize);
            return Ok(ApiResponseDto<PagedResponseDto<LoanApplicationResponseDto>>.SuccessResponse(
                result, "Applications retrieved successfully."));
        }

        /// <summary>
        /// Gets a single loan application by ID.
        /// Admin can view any application; Applicant can only view their own.
        /// </summary>
        /// <param name="id">The application GUID.</param>
        /// <returns>The application with full status history.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            Guid userId = GetUserIdFromClaims();
            string role = GetRoleFromClaims();
            LoanApplicationResponseDto result = await _service.GetByIdAsync(id, userId, role);
            return Ok(ApiResponseDto<LoanApplicationResponseDto>.SuccessResponse(
                result, "Application retrieved successfully."));
        }

        /// <summary>
        /// Returns the full status audit trail for a loan application.
        /// </summary>
        /// <param name="id">The application GUID.</param>
        /// <returns>List of status history entries.</returns>
        [HttpGet("{id:guid}/status")]
        public async Task<IActionResult> GetStatusHistory(Guid id)
        {
            Guid userId = GetUserIdFromClaims();
            string role = GetRoleFromClaims();
            List<StatusHistoryResponseDto> result =
                await _service.GetStatusHistoryAsync(id, userId, role);
            return Ok(ApiResponseDto<List<StatusHistoryResponseDto>>.SuccessResponse(
                result, "Status history retrieved successfully."));
        }

        /// <summary>Extracts the authenticated user's GUID from JWT claims.</summary>
        private Guid GetUserIdFromClaims()
        {
            string? userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                throw new UnauthorizedAccessException("Invalid token: user ID claim is missing or malformed.");
            }

            return userId;
        }

        /// <summary>Extracts the authenticated user's email from JWT claims.</summary>
        private string GetEmailFromClaims()
        {
            string email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email")
                ?? string.Empty;
            return email;
        }

        /// <summary>Extracts the authenticated user's role from JWT claims.</summary>
        private string GetRoleFromClaims()
        {
            string role = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? string.Empty;
            return role;
        }
    }
}
