using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.ApplicationService.Models
{
    /// <summary>
    /// Audit trail record for every status change on a loan application.
    /// A new record is created every time the application status changes.
    /// Maps to core.StatusHistory table in CapFinLoan_Loan database.
    /// </summary>
    [Table("StatusHistory", Schema = "core")]
    public class StatusHistory
    {
        /// <summary>Unique identifier for this history record</summary>
        [Key]
        public Guid HistoryId { get; set; } = Guid.NewGuid();

        /// <summary>The application this history belongs to</summary>
        [Required]
        public Guid ApplicationId { get; set; }

        /// <summary>Status before this change</summary>
        public ApplicationStatus FromStatus { get; set; }

        /// <summary>Status after this change</summary>
        public ApplicationStatus ToStatus { get; set; }

        /// <summary>
        /// Reason or note for this status change.
        /// Required when rejecting or requesting re-upload.
        /// </summary>
        [MaxLength(500)]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// Email of the person who made this status change.
        /// Applicant email for Draft→Submitted.
        /// Admin email for all other transitions.
        /// </summary>
        [Required]
        [MaxLength(150)]
        public string ChangedBy { get; set; } = string.Empty;

        /// <summary>UTC timestamp of this status change</summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // ── Navigation Properties ────────────────────────────

        /// <summary>The parent loan application</summary>
        [ForeignKey("ApplicationId")]
        public LoanApplication LoanApplication { get; set; } = null!;
    }
}
