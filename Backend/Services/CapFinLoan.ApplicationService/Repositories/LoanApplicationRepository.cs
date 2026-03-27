using CapFinLoan.ApplicationService.Data;
using CapFinLoan.ApplicationService.Models;
using CapFinLoan.ApplicationService.Repositories.Interfaces;
using CapFinLoan.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.ApplicationService.Repositories
{
    /// <summary>
    /// EF Core implementation of ILoanApplicationRepository.
    /// This is the ONLY layer that touches ApplicationDbContext directly.
    /// </summary>
    public class LoanApplicationRepository : ILoanApplicationRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="LoanApplicationRepository"/>.
        /// </summary>
        /// <param name="context">The EF Core database context.</param>
        public LoanApplicationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a loan application by its ID, including full status history ordered chronologically.
        /// </summary>
        public async Task<LoanApplication?> GetByIdAsync(Guid applicationId)
        {
            return await _context.LoanApplications
                .AsNoTracking()
                .Include(a => a.StatusHistories.OrderBy(h => h.ChangedAt))
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
        }

        /// <summary>
        /// Retrieves a loan application by ID and UserId — enforces applicant ownership.
        /// </summary>
        public async Task<LoanApplication?> GetByIdAndUserIdAsync(Guid applicationId, Guid userId)
        {
            return await _context.LoanApplications
                .AsNoTracking()
                .Include(a => a.StatusHistories.OrderBy(h => h.ChangedAt))
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId && a.UserId == userId);
        }

        /// <summary>
        /// Returns paginated applications for a specific user, ordered by CreatedAt descending.
        /// StatusHistories are NOT loaded for performance.
        /// </summary>
        public async Task<(List<LoanApplication> Applications, int TotalCount)> GetByUserIdAsync(
            Guid userId, int page, int pageSize)
        {
            IQueryable<LoanApplication> query = _context.LoanApplications
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt);

            int totalCount = await query.CountAsync();

            List<LoanApplication> applications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (applications, totalCount);
        }

        /// <summary>
        /// Returns paginated applications for admin view with optional status filter, ordered by CreatedAt descending.
        /// </summary>
        public async Task<(List<LoanApplication> Applications, int TotalCount)> GetAllAsync(
            int page, int pageSize, ApplicationStatus? statusFilter = null)
        {
            IQueryable<LoanApplication> query = _context.LoanApplications
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt);

            if (statusFilter.HasValue)
            {
                query = query.Where(a => a.Status == statusFilter.Value);
            }

            int totalCount = await query.CountAsync();

            List<LoanApplication> applications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (applications, totalCount);
        }

        /// <summary>
        /// Persists a new loan application to the database.
        /// </summary>
        public async Task<LoanApplication> CreateAsync(LoanApplication application)
        {
            await _context.LoanApplications.AddAsync(application);
            await _context.SaveChangesAsync();
            return application;
        }

        /// <summary>
        /// Persists updates to an existing loan application.
        /// </summary>
        public async Task<LoanApplication> UpdateAsync(LoanApplication application)
        {
            _context.LoanApplications.Update(application);
            await _context.SaveChangesAsync();
            return application;
        }

        /// <summary>
        /// Adds a new status history record to the audit trail.
        /// </summary>
        public async Task AddStatusHistoryAsync(StatusHistory history)
        {
            await _context.StatusHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Returns the complete status history for an application ordered by ChangedAt ascending.
        /// </summary>
        public async Task<List<StatusHistory>> GetStatusHistoryAsync(Guid applicationId)
        {
            return await _context.StatusHistories
                .AsNoTracking()
                .Where(h => h.ApplicationId == applicationId)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Returns the count of applications in a given status.
        /// </summary>
        public async Task<int> GetTotalCountByStatusAsync(ApplicationStatus status)
        {
            return await _context.LoanApplications
                .CountAsync(a => a.Status == status);
        }

        /// <summary>
        /// Saves an outbox message to the database.
        /// </summary>
        public async Task SaveOutboxMessageAsync(OutboxMessage message)
        {
            await _context.OutboxMessages.AddAsync(message);
            await _context.SaveChangesAsync();
        }
    }
}
