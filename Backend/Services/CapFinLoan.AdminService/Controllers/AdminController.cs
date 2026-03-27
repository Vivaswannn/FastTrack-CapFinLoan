using System.Security.Claims;
using CapFinLoan.AdminService.DTOs.Requests;
using CapFinLoan.AdminService.Features.Commands;
using CapFinLoan.AdminService.Features.Queries;
using CapFinLoan.SharedKernel.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.AdminService.Controllers
{
    /// <summary>
    /// Admin decision, reporting, and application queue endpoints.
    /// Implements CQRS via MediatR — all HTTP requests are dispatched
    /// as Queries (reads) or Commands (writes) to their respective handlers.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IMediator mediator,
            ILogger<AdminController> logger)
        {
            _mediator = mediator;
            _logger   = logger;
        }

        // ── Claims helpers ───────────────────────────────────────────────────

        private Guid GetUserIdFromClaims()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");
            return Guid.Parse(sub!);
        }

        private string GetAdminEmailFromClaims()
        {
            return User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email")
                ?? string.Empty;
        }

        private string GetTokenFromRequest()
        {
            var auth = HttpContext.Request.Headers["Authorization"].ToString();
            return auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? auth.Substring(7)
                : auth;
        }

        // ── Endpoints ────────────────────────────────────────────────────────

        /// <summary>
        /// Approve or reject a loan application.
        /// Dispatches <see cref="MakeDecisionCommand"/> via MediatR.
        /// Calculates EMI server-side and triggers status update in ApplicationService.
        /// </summary>
        [HttpPost("applications/{id:guid}/decision")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MakeDecision(
            [FromRoute] Guid id,
            [FromBody] MakeDecisionDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponseDto<object>.FailureResponse(
                    "Validation failed.", errors));
            }

            var adminId    = GetUserIdFromClaims();
            var adminEmail = GetAdminEmailFromClaims();
            var token      = GetTokenFromRequest();

            var result = await _mediator.Send(
                new MakeDecisionCommand(id, dto, adminId, adminEmail, token));

            return Ok(ApiResponseDto<object>.SuccessResponse(
                result, "Decision recorded successfully."));
        }

        /// <summary>
        /// Retrieve the decision for a specific loan application.
        /// Dispatches <see cref="GetDecisionByApplicationQuery"/> via MediatR.
        /// Accessible to the applicant as well.
        /// </summary>
        [HttpGet("decisions/{appId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDecision([FromRoute] Guid appId)
        {
            var decision = await _mediator.Send(
                new GetDecisionByApplicationQuery(appId));

            if (decision is null)
                return Ok(ApiResponseDto<object?>.SuccessResponse(
                    null, "No decision found for this application."));

            return Ok(ApiResponseDto<object>.SuccessResponse(
                decision, "Decision retrieved successfully."));
        }

        /// <summary>
        /// Returns aggregated KPI statistics for the admin dashboard.
        /// Dispatches <see cref="GetDashboardStatsQuery"/> via MediatR.
        /// </summary>
        [HttpGet("reports/dashboard")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
        {
            var stats = await _mediator.Send(new GetDashboardStatsQuery());
            return Ok(ApiResponseDto<object>.SuccessResponse(
                stats, "Dashboard statistics retrieved successfully."));
        }

        /// <summary>
        /// Returns a report summary combining dashboard stats and recent monthly trend.
        /// </summary>
        [HttpGet("reports/summary")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReportSummary()
        {
            var stats = await _mediator.Send(new GetDashboardStatsQuery());
            var trend = await _mediator.Send(new GetMonthlyTrendQuery(6));

            var summary = new
            {
                dashboard    = stats,
                monthlyTrend = trend,
                generatedAt  = DateTime.UtcNow
            };

            return Ok(ApiResponseDto<object>.SuccessResponse(
                summary, "Report summary retrieved successfully."));
        }

        /// <summary>
        /// Returns monthly decision counts for the trend line chart.
        /// Dispatches <see cref="GetMonthlyTrendQuery"/> via MediatR.
        /// Defaults to the last 6 months.
        /// </summary>
        [HttpGet("reports/monthly")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMonthlyTrend(
            [FromQuery] int months = 6)
        {
            var trend = await _mediator.Send(new GetMonthlyTrendQuery(months));
            return Ok(ApiResponseDto<object>.SuccessResponse(
                trend, "Monthly trend retrieved successfully."));
        }

        /// <summary>
        /// Proxies the paginated application queue from ApplicationService.
        /// Dispatches <see cref="GetApplicationQueueQuery"/> via MediatR.
        /// Accepts <see cref="PaginationFilter"/> for page/pageSize binding.
        /// </summary>
        [HttpGet("applications")]
        [HttpGet("queue")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApplicationQueue(
            [FromQuery] PaginationFilter filter,
            [FromQuery] string? status = null)
        {
            var token  = GetTokenFromRequest();
            var result = await _mediator.Send(
                new GetApplicationQueueQuery(filter.PageNumber, filter.PageSize, status, token));

            return Content(result, "application/json");
        }

        /// <summary>
        /// Exports all decisions (or a date-filtered subset) as a CSV file download.
        /// Dispatches <see cref="ExportDecisionsCsvCommand"/> via MediatR.
        /// </summary>
        [HttpGet("reports/export")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportCsv(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var bytes = await _mediator.Send(
                new ExportDecisionsCsvCommand(startDate, endDate));

            return File(bytes, "text/csv",
                $"decisions_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
