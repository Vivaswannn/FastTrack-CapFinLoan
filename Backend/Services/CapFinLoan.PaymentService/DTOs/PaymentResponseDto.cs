namespace CapFinLoan.PaymentService.DTOs
{
    /// <summary>Response DTO returned by PaymentService REST endpoints.</summary>
    public class PaymentResponseDto
    {
        /// <summary>Unique payment identifier.</summary>
        public Guid PaymentId { get; set; }

        /// <summary>The related loan application ID.</summary>
        public Guid ApplicationId { get; set; }

        /// <summary>The applicant user ID.</summary>
        public Guid UserId { get; set; }

        /// <summary>Amount disbursed.</summary>
        public decimal AmountDisbursed { get; set; }

        /// <summary>Payment status (Pending, Processing, Completed, Failed).</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Disbursement transaction reference number.</summary>
        public string? ReferenceNumber { get; set; }

        /// <summary>Result message.</summary>
        public string? Message { get; set; }

        /// <summary>When the payment was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>When the payment was processed.</summary>
        public DateTime? ProcessedAt { get; set; }
    }
}
