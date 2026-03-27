using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.ApplicationService.Models
{
    /// <summary>
    /// Represents a loan application submitted by an applicant.
    /// Maps to core.LoanApplications table in CapFinLoan_Loan database.
    ///
    /// Status lifecycle:
    /// Draft → Submitted → DocsPending → DocsVerified
    ///       → UnderReview → Approved/Rejected → Closed
    /// </summary>
    [Table("LoanApplications", Schema = "core")]
    public class LoanApplication
    {
        /// <summary>Unique identifier for this application</summary>
        [Key]
        public Guid ApplicationId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// ID of the applicant who created this application.
        /// This is a cross-service reference — NOT a foreign key.
        /// ApplicationService does not have direct access to auth.Users.
        /// If we need user details we call AuthService API.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>Type of loan being applied for</summary>
        [Required]
        public LoanType LoanType { get; set; }

        /// <summary>
        /// Requested loan amount in INR.
        /// Must be between 10,000 and 10,000,000.
        /// </summary>
        [Required]
        [Range(10000, 10000000)]
        public decimal LoanAmount { get; set; }

        /// <summary>
        /// Loan repayment period in months.
        /// Must be between 6 and 360 months (6 months to 30 years).
        /// </summary>
        [Required]
        [Range(6, 360)]
        public int TenureMonths { get; set; }

        /// <summary>Purpose of the loan</summary>
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        // ── Personal Information ─────────────────────────────
        // Copied from user profile at submission time.
        // We copy instead of referencing because user data
        // can change but application data must be immutable.

        /// <summary>Applicant full name at time of submission</summary>
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Applicant email at time of submission</summary>
        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Applicant phone at time of submission</summary>
        [MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        /// <summary>Date of birth for eligibility check</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Applicant residential address</summary>
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        // ── Employment Information ───────────────────────────

        /// <summary>Name of current employer</summary>
        [MaxLength(100)]
        public string EmployerName { get; set; } = string.Empty;

        /// <summary>Employment type: Salaried or Self-Employed</summary>
        [MaxLength(50)]
        public string EmploymentType { get; set; } = string.Empty;

        /// <summary>Current job title or designation</summary>
        [MaxLength(100)]
        public string JobTitle { get; set; } = string.Empty;

        /// <summary>Monthly gross income in INR</summary>
        public decimal MonthlyIncome { get; set; }

        /// <summary>Total years of work experience</summary>
        public int YearsOfExperience { get; set; }

        /// <summary>Employer office address</summary>
        [MaxLength(500)]
        public string EmployerAddress { get; set; } = string.Empty;

        // ── Status ──────────────────────────────────────────

        /// <summary>
        /// Current status of the application.
        /// Stored as string in database for readability.
        /// Transitions are strictly validated in service layer.
        /// </summary>
        public ApplicationStatus Status { get; set; }
            = ApplicationStatus.Draft;

        // ── Timestamps ──────────────────────────────────────

        /// <summary>When this application was first created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When this application was last modified</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>When applicant clicked Submit button</summary>
        public DateTime? SubmittedAt { get; set; }

        /// <summary>When admin made the final decision</summary>
        public DateTime? DecidedAt { get; set; }

        // ── Navigation Properties ────────────────────────────

        /// <summary>
        /// Complete audit trail of all status changes.
        /// Every status transition creates a new StatusHistory record.
        /// </summary>
        public ICollection<StatusHistory> StatusHistories { get; set; }
            = new List<StatusHistory>();
    }
}
