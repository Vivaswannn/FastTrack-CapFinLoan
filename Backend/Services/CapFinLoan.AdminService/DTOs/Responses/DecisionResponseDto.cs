namespace CapFinLoan.AdminService.DTOs.Responses
{
    /// <summary>
    /// Response DTO for a loan decision — returned to both admin and applicant.
    /// </summary>
    public class DecisionResponseDto
    {
        /// <summary>Unique decision identifier.</summary>
        public Guid DecisionId { get; set; }

        /// <summary>The loan application this decision relates to.</summary>
        public Guid ApplicationId { get; set; }

        /// <summary>Decision outcome as string: "Approved" or "Rejected".</summary>
        public string DecisionType { get; set; } = string.Empty;

        /// <summary>Admin remarks explaining the decision.</summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>Sanction terms (populated for approvals only).</summary>
        public string? SanctionTerms { get; set; }

        /// <summary>Approved loan amount (populated for approvals only).</summary>
        public decimal? LoanAmountApproved { get; set; }

        /// <summary>Annual interest rate applied (populated for approvals only).</summary>
        public decimal? InterestRate { get; set; }

        /// <summary>Repayment tenure in months (populated for approvals only).</summary>
        public int? TenureMonths { get; set; }

        /// <summary>Calculated monthly EMI (populated for approvals only).</summary>
        public decimal? MonthlyEmi { get; set; }

        /// <summary>Email of the admin who made this decision.</summary>
        public string DecidedBy { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the decision was recorded.</summary>
        public DateTime DecidedAt { get; set; }
    }
}
