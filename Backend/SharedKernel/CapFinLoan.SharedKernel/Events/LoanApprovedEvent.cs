namespace CapFinLoan.SharedKernel.Events
{
    /// <summary>
    /// Event published by AdminService to RabbitMQ when a loan application
    /// is approved. Consumed by PaymentService to initiate loan disbursement
    /// as part of the Saga choreography pattern.
    /// </summary>
    public class LoanApprovedEvent
    {
        /// <summary>Unique event identifier for idempotency checks.</summary>
        public Guid EventId { get; set; } = Guid.NewGuid();

        /// <summary>The approved loan application ID.</summary>
        public Guid ApplicationId { get; set; }

        /// <summary>The applicant's user ID.</summary>
        public Guid UserId { get; set; }

        /// <summary>Applicant email address.</summary>
        public string ApplicantEmail { get; set; } = string.Empty;

        /// <summary>Applicant full name.</summary>
        public string ApplicantName { get; set; } = string.Empty;

        /// <summary>The approved loan amount.</summary>
        public decimal LoanAmountApproved { get; set; }

        /// <summary>Approved interest rate (annual %).</summary>
        public decimal InterestRate { get; set; }

        /// <summary>Approved repayment tenure in months.</summary>
        public int TenureMonths { get; set; }

        /// <summary>Calculated monthly EMI amount.</summary>
        public decimal MonthlyEmi { get; set; }

        /// <summary>Admin who approved the loan.</summary>
        public string ApprovedBy { get; set; } = string.Empty;

        /// <summary>When this event was published.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
