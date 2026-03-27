using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.AdminService.DTOs.Requests
{
    /// <summary>
    /// Request body for admin approve/reject decision.
    /// MonthlyEmi is NOT here — it is calculated server-side from the
    /// LoanAmountApproved, InterestRate and TenureMonths.
    /// </summary>
    public class MakeDecisionDto
    {
        /// <summary>"Approved" or "Rejected" as a plain string to avoid enum binding issues.</summary>
        [Required(ErrorMessage = "DecisionType is required.")]
        public string DecisionType { get; set; } = string.Empty;

        /// <summary>Admin's explanation (required for both outcomes).</summary>
        [Required(ErrorMessage = "Remarks are required.")]
        [MaxLength(1000, ErrorMessage = "Remarks must not exceed 1000 characters.")]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>Sanction terms — required when Approved.</summary>
        [MaxLength(2000, ErrorMessage = "SanctionTerms must not exceed 2000 characters.")]
        public string? SanctionTerms { get; set; }

        /// <summary>Approved principal amount — required when Approved.</summary>
        public decimal? LoanAmountApproved { get; set; }

        /// <summary>Annual interest rate (e.g. 10.5 = 10.5%) — required when Approved.</summary>
        public decimal? InterestRate { get; set; }

        /// <summary>Repayment tenure in months — required when Approved.</summary>
        public int? TenureMonths { get; set; }
    }
}
