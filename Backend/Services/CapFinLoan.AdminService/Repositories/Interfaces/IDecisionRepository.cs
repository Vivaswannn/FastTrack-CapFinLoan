using CapFinLoan.AdminService.Models;

namespace CapFinLoan.AdminService.Repositories.Interfaces
{
    /// <summary>
    /// Data-access interface for the Decision entity.
    /// All methods are asynchronous.
    /// </summary>
    public interface IDecisionRepository
    {
        /// <summary>Returns a decision by its primary key, or null.</summary>
        Task<Decision?> GetByIdAsync(Guid decisionId);

        /// <summary>
        /// Returns the single decision for a given application, or null if none exists yet.
        /// One decision per application enforced at the database level.
        /// </summary>
        Task<Decision?> GetByApplicationIdAsync(Guid applicationId);

        /// <summary>Persists a new decision record and returns it.</summary>
        Task<Decision> CreateAsync(Decision decision);

        /// <summary>Total number of decisions across all applications.</summary>
        Task<int> GetTotalCountAsync();

        /// <summary>Count of decisions with outcome Approved.</summary>
        Task<int> GetApprovedCountAsync();

        /// <summary>Count of decisions with outcome Rejected.</summary>
        Task<int> GetRejectedCountAsync();

        /// <summary>Sum of all approved loan amounts.</summary>
        Task<decimal> GetTotalApprovedAmountAsync();

        /// <summary>All decisions, unfiltered, for CSV export.</summary>
        Task<List<Decision>> GetAllAsync();

        /// <summary>Decisions within an inclusive date range.</summary>
        Task<List<Decision>> GetByDateRangeAsync(DateTime start, DateTime end);

        /// <summary>
        /// Decisions from the last <paramref name="months"/> calendar months,
        /// ordered chronologically ascending.
        /// </summary>
        Task<List<Decision>> GetMonthlyDecisionsAsync(int months);
    }
}
