namespace CapFinLoan.SharedKernel.Events
{
    /// <summary>
    /// Event published by PaymentService after processing a loan disbursement.
    /// Consumed by ApplicationService to close the loan (Success) or
    /// revert to UnderReview (Failure) as part of the Saga choreography pattern.
    /// </summary>
    public class PaymentProcessedEvent
    {
        /// <summary>Unique event identifier for idempotency checks.</summary>
        public Guid EventId { get; set; } = Guid.NewGuid();

        /// <summary>The Payment record ID created by PaymentService.</summary>
        public Guid PaymentId { get; set; }

        /// <summary>The loan application this payment belongs to.</summary>
        public Guid ApplicationId { get; set; }

        /// <summary>The applicant's user ID.</summary>
        public Guid UserId { get; set; }

        /// <summary>Whether the payment processing succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>The disbursed amount (null on failure).</summary>
        public decimal? AmountDisbursed { get; set; }

        /// <summary>Human-readable result message.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>When this event was published.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
