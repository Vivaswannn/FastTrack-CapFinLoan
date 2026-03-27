namespace CapFinLoan.AdminService.DTOs.Responses
{
    /// <summary>
    /// Decisions grouped by calendar month.
    /// Used for the monthly trend line chart in the React dashboard.
    /// </summary>
    public class MonthlyTrendDto
    {
        /// <summary>Human-readable month label, e.g. "Jan 2025".</summary>
        public string Month { get; set; } = string.Empty;

        /// <summary>Calendar year (e.g. 2025).</summary>
        public int Year { get; set; }

        /// <summary>Calendar month number 1-12.</summary>
        public int MonthNumber { get; set; }

        /// <summary>Number of approvals in this month.</summary>
        public int ApprovedCount { get; set; }

        /// <summary>Number of rejections in this month.</summary>
        public int RejectedCount { get; set; }

        /// <summary>Total decisions (approved + rejected) in this month.</summary>
        public int TotalDecisions { get; set; }
    }
}
