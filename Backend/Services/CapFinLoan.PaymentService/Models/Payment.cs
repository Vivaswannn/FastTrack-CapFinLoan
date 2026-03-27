using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapFinLoan.PaymentService.Models
{
    /// <summary>
    /// Represents a loan disbursement payment record.
    /// Created when PaymentService processes a LoanApprovedEvent.
    /// </summary>
    [Table("Payments", Schema = "pay")]
    public class Payment
    {
        /// <summary>Unique payment identifier.</summary>
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();

        /// <summary>The approved loan application ID (cross-service plain Guid).</summary>
        [Required]
        public Guid ApplicationId { get; set; }

        /// <summary>The applicant user ID (cross-service plain Guid).</summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>The disbursed amount.</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountDisbursed { get; set; }

        /// <summary>Payment status: Pending, Processing, Completed, Failed.</summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>Reference number for the disbursement transaction.</summary>
        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        /// <summary>Human-readable result message.</summary>
        [MaxLength(500)]
        public string? Message { get; set; }

        /// <summary>When the payment record was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the payment was last updated.</summary>
        public DateTime? ProcessedAt { get; set; }
    }
}
