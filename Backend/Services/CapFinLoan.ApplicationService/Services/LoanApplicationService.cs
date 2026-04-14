using CapFinLoan.ApplicationService.DTOs.Requests;
using CapFinLoan.ApplicationService.DTOs.Responses;
using CapFinLoan.ApplicationService.Hubs;
using CapFinLoan.ApplicationService.Messaging;
using CapFinLoan.ApplicationService.Models;
using CapFinLoan.ApplicationService.Repositories.Interfaces;
using CapFinLoan.ApplicationService.Services.Interfaces;
using CapFinLoan.SharedKernel.DTOs;
using CapFinLoan.SharedKernel.Enums;
using CapFinLoan.SharedKernel.Events;
using CapFinLoan.SharedKernel.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CapFinLoan.ApplicationService.Services
{
    /// <summary>
    /// Service implementation for loan application business logic.
    /// Enforces the strict status machine, ownership rules, and audit trail.
    /// </summary>
    public class LoanApplicationService : ILoanApplicationService
    {
        private readonly ILoanApplicationRepository _repository;
        private readonly ILogger<LoanApplicationService> _logger;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IHubContext<LoanStatusHub> _hubContext;

        /// <summary>
        /// Initializes a new instance of <see cref="LoanApplicationService"/>.
        /// </summary>
        /// <param name="repository">The loan application repository.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="messagePublisher">The RabbitMQ message publisher.</param>
        /// <param name="hubContext">SignalR hub context for real-time push.</param>
        public LoanApplicationService(
            ILoanApplicationRepository repository,
            ILogger<LoanApplicationService> logger,
            IMessagePublisher messagePublisher,
            IHubContext<LoanStatusHub> hubContext)
        {
            _repository = repository;
            _logger = logger;
            _messagePublisher = messagePublisher;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Creates a new loan application with Status=Draft.
        /// Also inserts the first StatusHistory record.
        /// </summary>
        public async Task<LoanApplicationResponseDto> CreateDraftAsync(
            Guid userId, CreateLoanApplicationDto dto)
        {
            _logger.LogInformation(
                "CreateDraft received: LoanType={LoanType}, Amount={Amount}, " +
                "TenureMonths={TenureMonths}, FullName={FullName}, Email={Email}, Phone={Phone}",
                dto.LoanType, dto.LoanAmount, dto.TenureMonths,
                dto.FullName, dto.Email, dto.Phone);

            LoanApplication application = new LoanApplication
            {
                UserId = userId,
                LoanType = dto.LoanType,
                LoanAmount = dto.LoanAmount,
                TenureMonths = dto.TenureMonths,
                Purpose = dto.Purpose ?? string.Empty,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone ?? string.Empty,
                DateOfBirth = dto.DateOfBirth,
                Address = dto.Address ?? string.Empty,
                EmployerName = dto.EmployerName ?? string.Empty,
                EmploymentType = dto.EmploymentType ?? string.Empty,
                JobTitle = dto.JobTitle ?? string.Empty,
                MonthlyIncome = dto.MonthlyIncome,
                YearsOfExperience = dto.YearsOfExperience,
                EmployerAddress = dto.EmployerAddress ?? string.Empty,
                Status = ApplicationStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            LoanApplication created = await _repository.CreateAsync(application);

            StatusHistory initialHistory = new StatusHistory
            {
                ApplicationId = created.ApplicationId,
                FromStatus = ApplicationStatus.Draft,
                ToStatus = ApplicationStatus.Draft,
                ChangedBy = userId.ToString(),
                ChangedAt = DateTime.UtcNow,
                Remarks = "Application created as draft"
            };
            await _repository.AddStatusHistoryAsync(initialHistory);

            _logger.LogInformation(
                "New loan application draft created: {ApplicationId} by UserId {UserId}",
                created.ApplicationId, userId);

            return MapToResponseDto(created);
        }

        /// <summary>
        /// Updates an existing draft application.
        /// Throws if not found, not owned by the user, or not in Draft status.
        /// </summary>
        public async Task<LoanApplicationResponseDto> UpdateDraftAsync(
            Guid applicationId, Guid userId, UpdateLoanApplicationDto dto)
        {
            LoanApplication? application = await _repository.GetByIdAndUserIdAsync(applicationId, userId);

            if (application is null)
            {
                throw new KeyNotFoundException($"Application {applicationId} not found.");
            }

            if (application.Status != ApplicationStatus.Draft)
            {
                throw new InvalidOperationException("Only draft applications can be updated.");
            }

            if (dto.LoanType.HasValue) application.LoanType = dto.LoanType.Value;
            if (dto.LoanAmount.HasValue) application.LoanAmount = dto.LoanAmount.Value;
            if (dto.TenureMonths.HasValue) application.TenureMonths = dto.TenureMonths.Value;
            if (dto.Purpose is not null) application.Purpose = dto.Purpose;
            if (dto.FullName is not null) application.FullName = dto.FullName;
            if (dto.Email is not null) application.Email = dto.Email;
            if (dto.Phone is not null) application.Phone = dto.Phone;
            if (dto.DateOfBirth.HasValue) application.DateOfBirth = dto.DateOfBirth;
            if (dto.Address is not null) application.Address = dto.Address;
            if (dto.EmployerName is not null) application.EmployerName = dto.EmployerName;
            if (dto.EmploymentType is not null) application.EmploymentType = dto.EmploymentType;
            if (dto.JobTitle is not null) application.JobTitle = dto.JobTitle;
            if (dto.MonthlyIncome.HasValue) application.MonthlyIncome = dto.MonthlyIncome.Value;
            if (dto.YearsOfExperience.HasValue) application.YearsOfExperience = dto.YearsOfExperience.Value;
            if (dto.EmployerAddress is not null) application.EmployerAddress = dto.EmployerAddress;

            application.UpdatedAt = DateTime.UtcNow;

            LoanApplication updated = await _repository.UpdateAsync(application);

            _logger.LogInformation("Loan application updated: {ApplicationId}", applicationId);

            return MapToResponseDto(updated);
        }

        /// <summary>
        /// Submits a draft application (Draft → Submitted).
        /// Validates required employment fields before allowing submission.
        /// </summary>
        public async Task<LoanApplicationResponseDto> SubmitAsync(
            Guid applicationId, Guid userId, string userEmail)
        {
            LoanApplication? application = await _repository.GetByIdAndUserIdAsync(applicationId, userId);

            if (application is null)
            {
                throw new KeyNotFoundException($"Application {applicationId} not found.");
            }

            if (application.Status != ApplicationStatus.Draft)
            {
                throw new InvalidOperationException("Only draft applications can be submitted.");
            }

            if (string.IsNullOrEmpty(application.EmployerName) || application.MonthlyIncome <= 0)
            {
                throw new ArgumentException(
                    "Please complete all employment details before submitting.");
            }

            application.Status = ApplicationStatus.Submitted;
            application.SubmittedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            LoanApplication updated = await _repository.UpdateAsync(application);

            StatusHistory history = new StatusHistory
            {
                ApplicationId = applicationId,
                FromStatus = ApplicationStatus.Draft,
                ToStatus = ApplicationStatus.Submitted,
                ChangedBy = userEmail,
                ChangedAt = DateTime.UtcNow,
                Remarks = "Application submitted by applicant"
            };
            await _repository.AddStatusHistoryAsync(history);

            _logger.LogInformation(
                "Application submitted: {ApplicationId} by {UserEmail}",
                applicationId, userEmail);

            var submitEvent = new LoanStatusChangedEvent
            {
                ApplicationId = applicationId,
                UserId = application.UserId,
                ApplicantEmail = application.Email,
                ApplicantName = application.FullName,
                OldStatus = "Draft",
                NewStatus = "Submitted",
                Remarks = "Application submitted by applicant",
                LoanType = application.LoanType.ToString(),
                LoanAmount = application.LoanAmount,
                Timestamp = DateTime.UtcNow
            };

            var outboxMsg = new OutboxMessage
            {
                EventType = nameof(LoanStatusChangedEvent), // Use LoanSubmittedEvent logic but class is LoanStatusChangedEvent
                Payload = System.Text.Json.JsonSerializer.Serialize(submitEvent)
            };
            
            await _repository.SaveOutboxMessageAsync(outboxMsg);

            return MapToResponseDto(updated);
        }

        /// <summary>
        /// Gets a single application.
        /// Admin can view any; Applicant can only view their own.
        /// </summary>
        public async Task<LoanApplicationResponseDto> GetByIdAsync(
            Guid applicationId, Guid userId, string role)
        {
            LoanApplication? application;

            if (role == "Admin")
            {
                application = await _repository.GetByIdAsync(applicationId);
            }
            else
            {
                application = await _repository.GetByIdAndUserIdAsync(applicationId, userId);
            }

            if (application is null)
            {
                throw new KeyNotFoundException("Application not found or access denied.");
            }

            return MapToResponseDto(application, includeHistory: true);
        }

        /// <summary>
        /// Returns a paginated list of applications owned by the authenticated applicant.
        /// </summary>
        public async Task<PagedResponseDto<LoanApplicationResponseDto>> GetMyApplicationsAsync(
            Guid userId, int page, int pageSize)
        {
            (List<LoanApplication> applications, int totalCount) =
                await _repository.GetByUserIdAsync(userId, page, pageSize);

            List<LoanApplicationResponseDto> dtos = applications
                .Select(a => MapToResponseDto(a))
                .ToList();

            return PaginationHelper.CreatePagedResponse(dtos, totalCount, page, pageSize);
        }

        /// <summary>
        /// Returns a paginated list of all applications for admin view.
        /// </summary>
        public async Task<PagedResponseDto<LoanApplicationResponseDto>> GetAllApplicationsAsync(
            int page, int pageSize, ApplicationStatus? statusFilter)
        {
            (List<LoanApplication> applications, int totalCount) =
                await _repository.GetAllAsync(page, pageSize, statusFilter);

            List<LoanApplicationResponseDto> dtos = applications
                .Select(a => MapToResponseDto(a))
                .ToList();

            return PaginationHelper.CreatePagedResponse(dtos, totalCount, page, pageSize);
        }

        /// <summary>
        /// Admin updates the status of an application.
        /// Strictly validates the transition against the status machine before applying.
        /// Creates a StatusHistory record on every successful transition.
        /// </summary>
        public async Task<LoanApplicationResponseDto> UpdateStatusAsync(
            Guid applicationId, UpdateApplicationStatusDto dto, string adminEmail)
        {
            LoanApplication? application = await _repository.GetByIdAsync(applicationId);

            if (application is null)
            {
                throw new KeyNotFoundException($"Application {applicationId} not found.");
            }

            ApplicationStatus fromStatus = application.Status;
            ApplicationStatus toStatus = dto.NewStatus;

            if (!IsValidAdminTransition(fromStatus, toStatus))
            {
                throw new InvalidOperationException(
                    $"Invalid status transition from {fromStatus} to {toStatus}. This transition is not allowed.");
            }

            application.Status = toStatus;
            application.UpdatedAt = DateTime.UtcNow;

            LoanApplication updated = await _repository.UpdateAsync(application);

            StatusHistory history = new StatusHistory
            {
                ApplicationId = applicationId,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedBy = adminEmail,
                ChangedAt = DateTime.UtcNow,
                Remarks = dto.Remarks ?? string.Empty
            };
            await _repository.AddStatusHistoryAsync(history);

            _logger.LogInformation(
                "Application {ApplicationId} status changed from {From} to {To} by {AdminEmail}",
                applicationId, fromStatus, toStatus, adminEmail);

            var statusEvent = new LoanStatusChangedEvent
            {
                ApplicationId = applicationId,
                UserId = application.UserId,
                ApplicantEmail = application.Email,
                ApplicantName = application.FullName,
                OldStatus = fromStatus.ToString(),
                NewStatus = toStatus.ToString(),
                Remarks = dto.Remarks ?? string.Empty,
                LoanType = application.LoanType.ToString(),
                LoanAmount = application.LoanAmount,
                Timestamp = DateTime.UtcNow
            };

            var outboxMsg = new OutboxMessage
            {
                EventType = nameof(LoanStatusChangedEvent),
                Payload = System.Text.Json.JsonSerializer.Serialize(statusEvent)
            };

            await _repository.SaveOutboxMessageAsync(outboxMsg);

            // Push real-time status update to all clients watching this application
            await _hubContext.Clients
                .Group(applicationId.ToString())
                .SendAsync("StatusUpdated", new
                {
                    applicationId,
                    oldStatus = fromStatus.ToString(),
                    newStatus = toStatus.ToString(),
                    changedBy = adminEmail,
                    changedAt = DateTime.UtcNow,
                    remarks   = dto.Remarks ?? string.Empty
                });

            _logger.LogInformation(
                "SignalR push sent for ApplicationId={ApplicationId} → {NewStatus}",
                applicationId, toStatus);

            return MapToResponseDto(updated);
        }

        /// <summary>
        /// Returns the full status audit trail for an application.
        /// Applicant can only view history for their own applications.
        /// </summary>
        public async Task<List<StatusHistoryResponseDto>> GetStatusHistoryAsync(
            Guid applicationId, Guid userId, string role)
        {
            if (role != "Admin")
            {
                LoanApplication? application = await _repository.GetByIdAndUserIdAsync(applicationId, userId);
                if (application is null)
                {
                    throw new KeyNotFoundException("Application not found or access denied.");
                }
            }

            List<StatusHistory> history = await _repository.GetStatusHistoryAsync(applicationId);

            return history.Select(h => new StatusHistoryResponseDto
            {
                HistoryId = h.HistoryId,
                ApplicationId = h.ApplicationId,
                FromStatus = h.FromStatus.ToString(),
                ToStatus = h.ToStatus.ToString(),
                Remarks = h.Remarks,
                ChangedBy = h.ChangedBy,
                ChangedAt = h.ChangedAt
            }).ToList();
        }

        /// <summary>
        /// Validates whether a status transition is allowed for an admin.
        /// </summary>
        /// <param name="from">Current status.</param>
        /// <param name="to">Desired new status.</param>
        /// <returns>True if the transition is permitted.</returns>
        private static bool IsValidAdminTransition(ApplicationStatus from, ApplicationStatus to)
        {
            return (from, to) switch
            {
                (ApplicationStatus.Submitted, ApplicationStatus.DocsPending) => true,
                (ApplicationStatus.DocsPending, ApplicationStatus.DocsVerified) => true,
                (ApplicationStatus.DocsVerified, ApplicationStatus.UnderReview) => true,
                (ApplicationStatus.UnderReview, ApplicationStatus.Approved) => true,
                (ApplicationStatus.UnderReview, ApplicationStatus.Rejected) => true,
                (ApplicationStatus.Approved, ApplicationStatus.Closed) => true,
                (ApplicationStatus.Rejected, ApplicationStatus.Closed) => true,
                _ => false
            };
        }

        /// <summary>
        /// Maps a LoanApplication model to a LoanApplicationResponseDto.
        /// Enums are converted to strings for client readability.
        /// </summary>
        /// <param name="application">The domain model to map.</param>
        /// <param name="includeHistory">
        /// When true, maps the loaded StatusHistories navigation property.
        /// When false (list endpoints), returns an empty list for performance.
        /// </param>
        /// <returns>The mapped response DTO.</returns>
        private static LoanApplicationResponseDto MapToResponseDto(
            LoanApplication application, bool includeHistory = false)
        {
            List<StatusHistoryResponseDto> histories = [];

            if (includeHistory && application.StatusHistories is not null)
            {
                histories = application.StatusHistories.Select(h => new StatusHistoryResponseDto
                {
                    HistoryId = h.HistoryId,
                    ApplicationId = h.ApplicationId,
                    FromStatus = h.FromStatus.ToString(),
                    ToStatus = h.ToStatus.ToString(),
                    Remarks = h.Remarks,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt
                }).ToList();
            }

            return new LoanApplicationResponseDto
            {
                ApplicationId = application.ApplicationId,
                UserId = application.UserId,
                LoanType = application.LoanType.ToString(),
                LoanAmount = application.LoanAmount,
                TenureMonths = application.TenureMonths,
                Purpose = application.Purpose,
                FullName = application.FullName,
                Email = application.Email,
                Phone = application.Phone,
                DateOfBirth = application.DateOfBirth,
                Address = application.Address,
                EmployerName = application.EmployerName,
                EmploymentType = application.EmploymentType,
                JobTitle = application.JobTitle,
                MonthlyIncome = application.MonthlyIncome,
                YearsOfExperience = application.YearsOfExperience,
                EmployerAddress = application.EmployerAddress,
                Status = application.Status.ToString(),
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt,
                SubmittedAt = application.SubmittedAt,
                StatusHistories = histories
            };
        }
    }
}
