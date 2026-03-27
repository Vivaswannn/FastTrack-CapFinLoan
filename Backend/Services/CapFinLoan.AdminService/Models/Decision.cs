using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.AdminService.Models
{
    /// <summary>
    /// Represents the final decision made by admin on a loan application.
    /// Created when admin clicks Approve or Reject.
    /// Maps to admin.Decisions table in CapFinLoan_Admin database.
    /// </summary>
    [Table("Decisions", Schema = "admin")]
    public class Decision
    {
        /// <summary>Unique identifier for this decision</summary>
        [Key]
        public Guid DecisionId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The loan application this decision is for.
        /// Cross-service reference — NOT a foreign key.
        /// AdminService does not share DB with ApplicationService.
        /// </summary>
        [Required]
        public Guid ApplicationId { get; set; }

        /// <summary>
        /// The applicant who owns this application.
        /// Stored here to avoid calling ApplicationService
        /// just to send notifications.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>Whether the application was approved or rejected</summary>
        [Required]
        public DecisionType DecisionType { get; set; }

        /// <summary>
        /// Admin's explanation for this decision.
        /// Required for both approvals and rejections.
        /// Applicant can see this.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// Terms and conditions of the approved loan.
        /// Only filled when DecisionType is Approved.
        /// Includes repayment schedule, interest details etc.
        /// </summary>
        [MaxLength(2000)]
        public string? SanctionTerms { get; set; }

        /// <summary>
        /// Final approved loan amount.
        /// May differ from requested amount.
        /// Only filled when Approved.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? LoanAmountApproved { get; set; }

        /// <summary>
        /// Annual interest rate for approved loan.
        /// Example: 8.5 means 8.5% per annum.
        /// Only filled when Approved.
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? InterestRate { get; set; }

        /// <summary>
        /// Approved repayment tenure in months.
        /// May differ from requested tenure.
        /// Only filled when Approved.
        /// </summary>
        public int? TenureMonths { get; set; }

        /// <summary>
        /// Monthly EMI amount.
        /// Calculated as: LoanAmount * InterestRate / (1-(1+rate)^-tenure)
        /// Only filled when Approved.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonthlyEmi { get; set; }

        /// <summary>Email of admin who made this decision</summary>
        [Required]
        [MaxLength(150)]
        public string DecidedBy { get; set; } = string.Empty;

        /// <summary>UTC timestamp of this decision</summary>
        public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
    }
}
