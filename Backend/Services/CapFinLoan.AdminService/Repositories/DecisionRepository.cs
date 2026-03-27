using CapFinLoan.AdminService.Data;
using CapFinLoan.AdminService.Models;
using CapFinLoan.AdminService.Repositories.Interfaces;
using CapFinLoan.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.AdminService.Repositories
{
    /// <summary>
    /// EF Core implementation of IDecisionRepository.
    /// Only layer that accesses AdminDbContext directly.
    /// </summary>
    public class DecisionRepository : IDecisionRepository
    {
        private readonly AdminDbContext _context;

        public DecisionRepository(AdminDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<Decision?> GetByIdAsync(Guid decisionId)
        {
            return await _context.Decisions
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DecisionId == decisionId);
        }

        /// <inheritdoc/>
        public async Task<Decision?> GetByApplicationIdAsync(Guid applicationId)
        {
            return await _context.Decisions
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.ApplicationId == applicationId);
        }

        /// <inheritdoc/>
        public async Task<Decision> CreateAsync(Decision decision)
        {
            _context.Decisions.Add(decision);
            await _context.SaveChangesAsync();
            return decision;
        }

        /// <inheritdoc/>
        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Decisions.CountAsync();
        }

        /// <inheritdoc/>
        public async Task<int> GetApprovedCountAsync()
        {
            return await _context.Decisions
                .CountAsync(d => d.DecisionType == DecisionType.Approved);
        }

        /// <inheritdoc/>
        public async Task<int> GetRejectedCountAsync()
        {
            return await _context.Decisions
                .CountAsync(d => d.DecisionType == DecisionType.Rejected);
        }

        /// <inheritdoc/>
        public async Task<decimal> GetTotalApprovedAmountAsync()
        {
            return await _context.Decisions
                .Where(d => d.DecisionType == DecisionType.Approved
                         && d.LoanAmountApproved.HasValue)
                .SumAsync(d => d.LoanAmountApproved ?? 0);
        }

        /// <inheritdoc/>
        public async Task<List<Decision>> GetAllAsync()
        {
            return await _context.Decisions
                .AsNoTracking()
                .OrderByDescending(d => d.DecidedAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Decision>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.Decisions
                .AsNoTracking()
                .Where(d => d.DecidedAt >= start && d.DecidedAt <= end)
                .OrderBy(d => d.DecidedAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<Decision>> GetMonthlyDecisionsAsync(int months)
        {
            var cutoff = DateTime.UtcNow.AddMonths(-months);
            return await _context.Decisions
                .AsNoTracking()
                .Where(d => d.DecidedAt >= cutoff)
                .OrderBy(d => d.DecidedAt)
                .ToListAsync();
        }
    }
}
