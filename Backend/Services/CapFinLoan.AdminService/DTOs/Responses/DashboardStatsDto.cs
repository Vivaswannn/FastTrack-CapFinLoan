namespace CapFinLoan.AdminService.DTOs.Responses
{
    /// <summary>
    /// Aggregated statistics for the admin dashboard.
    /// Drives charts and KPI tiles in the React frontend.
    /// </summary>
    public class DashboardStatsDto
    {
        /// <summary>Total decisions recorded in AdminService.</summary>
        public int TotalApplications { get; set; }

        /// <summary>Count of decisions where the application was submitted.</summary>
        public int SubmittedCount { get; set; }

        /// <summary>Count of decisions currently under review.</summary>
        public int UnderReviewCount { get; set; }

        /// <summary>Count of approved decisions.</summary>
        public int ApprovedCount { get; set; }

        /// <summary>Count of rejected decisions.</summary>
        public int RejectedCount { get; set; }

        /// <summary>Applications not yet approved or rejected.</summary>
        public int PendingCount { get; set; }

        /// <summary>Approval percentage e.g. 75.50 means 75.5%.</summary>
        public decimal ApprovalRate { get; set; }

        /// <summary>Sum of all approved loan amounts.</summary>
        public decimal TotalLoanAmountApproved { get; set; }

        /// <summary>Average approved loan amount (0 when no approvals).</summary>
        public decimal AverageLoanAmount { get; set; }
    }
}
