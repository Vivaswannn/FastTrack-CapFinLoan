using System.Text;
using CapFinLoan.AdminService.DTOs.Requests;
using CapFinLoan.AdminService.DTOs.Responses;
using CapFinLoan.AdminService.Helpers;
using CapFinLoan.AdminService.Messaging;
using CapFinLoan.AdminService.Models;
using CapFinLoan.AdminService.Repositories.Interfaces;
using CapFinLoan.AdminService.Services.Interfaces;
using CapFinLoan.SharedKernel.Enums;
using CapFinLoan.SharedKernel.Events;

namespace CapFinLoan.AdminService.Services
{
    /// <summary>
    /// Implements all admin decision and reporting business logic.
    /// Stores decisions in AdminService DB and proxies application queue
    /// from ApplicationService via HTTP.
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly IDecisionRepository _decisionRepository;
        private readonly IApplicationHttpService _applicationHttpService;
        private readonly ILogger<AdminService> _logger;
        private readonly ICacheService _cacheService;
        private readonly ILoanApprovedPublisher _loanApprovedPublisher;

        private const string DashboardStatsCacheKey = "dashboard:stats";
        private const string MonthlyTrendCacheKeyPrefix = "report:monthly:";

        public AdminService(
            IDecisionRepository decisionRepository,
            IApplicationHttpService applicationHttpService,
            ILogger<AdminService> logger,
            ICacheService cacheService,
            ILoanApprovedPublisher loanApprovedPublisher)
        {
            _decisionRepository       = decisionRepository;
            _applicationHttpService   = applicationHttpService;
            _logger                   = logger;
            _cacheService             = cacheService;
            _loanApprovedPublisher    = loanApprovedPublisher;
        }

        /// <inheritdoc/>
        public async Task<DecisionResponseDto> MakeDecisionAsync(
            Guid applicationId,
            MakeDecisionDto dto,
            Guid adminId,
            string adminEmail,
            string adminToken)
        {
            // Step 1: Prevent duplicate decisions
            var existing = await _decisionRepository
                .GetByApplicationIdAsync(applicationId);

            if (existing != null)
                throw new InvalidOperationException(
                    "A decision has already been made for this application.");

            // Step 2: Validate application is in UnderReview status
            var currentStatus = await _applicationHttpService
                .GetApplicationStatusAsync(applicationId, adminToken);
            if (currentStatus != null && currentStatus != "UnderReview")
            {
                throw new InvalidOperationException(
                    $"Cannot make a decision on an application in '{currentStatus}' status. " +
                    "Application must be in 'UnderReview' status.");
            }

            // Step 3: Calculate EMI for approvals
            decimal? monthlyEmi = null;
            if (dto.DecisionType == "Approved"
                && dto.LoanAmountApproved.HasValue
                && dto.InterestRate.HasValue
                && dto.TenureMonths.HasValue)
            {
                monthlyEmi = EmiCalculator.CalculateEmi(
                    dto.LoanAmountApproved.Value,
                    dto.InterestRate.Value,
                    dto.TenureMonths.Value);
            }

            // Step 3: Persist decision to AdminService database
            var decision = new Decision
            {
                ApplicationId      = applicationId,
                UserId             = adminId,
                DecisionType       = Enum.Parse<DecisionType>(dto.DecisionType),
                Remarks            = dto.Remarks,
                SanctionTerms      = dto.SanctionTerms,
                LoanAmountApproved = dto.LoanAmountApproved,
                InterestRate       = dto.InterestRate,
                TenureMonths       = dto.TenureMonths,
                MonthlyEmi         = monthlyEmi,
                DecidedBy          = adminEmail,
                DecidedAt          = DateTime.UtcNow
            };

            var saved = await _decisionRepository.CreateAsync(decision);

            // Step 4: Update application status in ApplicationService
            var newStatus = dto.DecisionType == "Approved"
                ? "Approved"
                : "Rejected";

            try
            {
                _logger.LogInformation(
                    "Attempting status update for {AppId} to {Status}",
                    applicationId, newStatus);

                await _applicationHttpService.UpdateApplicationStatusAsync(
                    applicationId, newStatus, dto.Remarks, adminToken);

                _logger.LogInformation(
                    "Status update call completed for {AppId}", applicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Status update FAILED for {AppId}: {Message}",
                    applicationId, ex.Message);
            }

            // Step 5: Publish LoanApprovedEvent for Saga choreography (PaymentService)
            if (dto.DecisionType == "Approved"
                && dto.LoanAmountApproved.HasValue
                && dto.InterestRate.HasValue
                && dto.TenureMonths.HasValue)
            {
                try
                {
                    var approvedEvent = new LoanApprovedEvent
                    {
                        ApplicationId      = applicationId,
                        UserId             = adminId,
                        ApplicantEmail     = string.Empty, // not available in AdminService
                        ApplicantName      = string.Empty,
                        LoanAmountApproved = dto.LoanAmountApproved.Value,
                        InterestRate       = dto.InterestRate.Value,
                        TenureMonths       = dto.TenureMonths.Value,
                        MonthlyEmi         = monthlyEmi ?? 0,
                        ApprovedBy         = adminEmail,
                        Timestamp          = DateTime.UtcNow
                    };

                    await _loanApprovedPublisher.PublishLoanApprovedAsync(approvedEvent);

                    _logger.LogInformation(
                        "LoanApprovedEvent published for Saga: ApplicationId={AppId}",
                        applicationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to publish LoanApprovedEvent for ApplicationId={AppId}",
                        applicationId);
                    // Do not rethrow — decision is already saved
                }
            }

            // Step 6: Invalidate dashboard cache after new decision
            await _cacheService.RemoveAsync(DashboardStatsCacheKey);
            await _cacheService.RemoveAsync(
                $"{MonthlyTrendCacheKeyPrefix}6");
            await _cacheService.RemoveAsync(
                $"{MonthlyTrendCacheKeyPrefix}12");
            _logger.LogInformation(
                "Dashboard cache invalidated after new decision");

            // Step 6: Log and return
            _logger.LogInformation(
                "Decision {DecisionType} made for application {ApplicationId} by {AdminEmail}",
                dto.DecisionType, applicationId, adminEmail);

            return MapToResponseDto(saved);
        }

        /// <inheritdoc/>
        public async Task<DecisionResponseDto?> GetDecisionByApplicationAsync(
            Guid applicationId)
        {
            var decision = await _decisionRepository
                .GetByApplicationIdAsync(applicationId);

            return decision is null ? null : MapToResponseDto(decision);
        }

        /// <inheritdoc/>
        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            // Try cache first
            var cached = await _cacheService
                .GetAsync<DashboardStatsDto>(DashboardStatsCacheKey);
            if (cached != null)
            {
                _logger.LogInformation(
                    "Dashboard stats served from cache");
                return cached;
            }

            // Cache miss — fetch from DB
            _logger.LogInformation(
                "Dashboard stats cache miss — fetching from DB");

            // Execute sequentially — EF Core DbContext is not thread-safe
            int approved = await _decisionRepository.GetApprovedCountAsync();
            int rejected = await _decisionRepository.GetRejectedCountAsync();
            int total    = await _decisionRepository.GetTotalCountAsync();
            decimal totalAmount = await _decisionRepository.GetTotalApprovedAmountAsync();

            decimal approvalRate = total > 0
                ? Math.Round((decimal)approved / total * 100, 1)
                : 0;

            decimal avgAmount = approved > 0
                ? Math.Round(totalAmount / approved, 0)
                : 0;

            var result = new DashboardStatsDto
            {
                TotalApplications      = total,
                ApprovedCount          = approved,
                RejectedCount          = rejected,
                PendingCount           = total - approved - rejected,
                SubmittedCount         = 0,
                UnderReviewCount       = 0,
                ApprovalRate           = approvalRate,
                TotalLoanAmountApproved = totalAmount,
                AverageLoanAmount      = avgAmount
            };

            // Cache the result for 5 minutes
            await _cacheService.SetAsync(
                DashboardStatsCacheKey,
                result,
                TimeSpan.FromMinutes(5));

            return result;
        }

        /// <inheritdoc/>
        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months)
        {
            var cacheKey = $"{MonthlyTrendCacheKeyPrefix}{months}";
            var cached = await _cacheService
                .GetAsync<List<MonthlyTrendDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation(
                    "Monthly trend served from cache for {Months} months", months);
                return cached;
            }

            var decisions = await _decisionRepository.GetMonthlyDecisionsAsync(months);

            var grouped = decisions
                .GroupBy(d => new { d.DecidedAt.Year, d.DecidedAt.Month })
                .Select(g => new MonthlyTrendDto
                {
                    Year           = g.Key.Year,
                    MonthNumber    = g.Key.Month,
                    Month          = new DateTime(g.Key.Year, g.Key.Month, 1)
                                         .ToString("MMM yyyy"),
                    ApprovedCount  = g.Count(d => d.DecisionType == DecisionType.Approved),
                    RejectedCount  = g.Count(d => d.DecisionType == DecisionType.Rejected),
                    TotalDecisions = g.Count()
                })
                .OrderBy(t => t.Year)
                .ThenBy(t => t.MonthNumber)
                .ToList();

            // Cache the result for 10 minutes
            await _cacheService.SetAsync(
                cacheKey, grouped, TimeSpan.FromMinutes(10));

            return grouped;
        }

        /// <inheritdoc/>
        public async Task<string> GetApplicationQueueAsync(
            int page,
            int pageSize,
            string? statusFilter,
            string adminToken)
        {
            var result = await _applicationHttpService
                .GetApplicationQueueAsync(page, pageSize, statusFilter, adminToken);

            return result ?? "{\"items\":[],\"totalCount\":0}";
        }

        /// <inheritdoc/>
        public async Task<byte[]> ExportDecisionsToCsvAsync(
            DateTime? startDate,
            DateTime? endDate)
        {
            List<Decision> decisions;

            if (startDate.HasValue && endDate.HasValue)
                decisions = await _decisionRepository
                    .GetByDateRangeAsync(startDate.Value, endDate.Value);
            else
                decisions = await _decisionRepository.GetAllAsync();

            var sb = new StringBuilder();
            sb.AppendLine(
                "DecisionId,ApplicationId,DecisionType,Remarks," +
                "LoanAmountApproved,InterestRate,TenureMonths,MonthlyEmi," +
                "DecidedBy,DecidedAt");

            foreach (var d in decisions)
            {
                sb.AppendLine(
                    $"{d.DecisionId},{d.ApplicationId}," +
                    $"{d.DecisionType},{d.Remarks?.Replace(",", ";")}," +
                    $"{d.LoanAmountApproved},{d.InterestRate}," +
                    $"{d.TenureMonths},{d.MonthlyEmi}," +
                    $"{d.DecidedBy},{d.DecidedAt:yyyy-MM-dd HH:mm:ss}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // ── Private helpers ─────────────────────────────────────────────────

        /// <summary>Maps a Decision domain model to a DecisionResponseDto.</summary>
        private static DecisionResponseDto MapToResponseDto(Decision d)
        {
            return new DecisionResponseDto
            {
                DecisionId         = d.DecisionId,
                ApplicationId      = d.ApplicationId,
                DecisionType       = d.DecisionType.ToString(),
                Remarks            = d.Remarks,
                SanctionTerms      = d.SanctionTerms,
                LoanAmountApproved = d.LoanAmountApproved,
                InterestRate       = d.InterestRate,
                TenureMonths       = d.TenureMonths,
                MonthlyEmi         = d.MonthlyEmi,
                DecidedBy          = d.DecidedBy,
                DecidedAt          = d.DecidedAt
            };
        }
    }
}
