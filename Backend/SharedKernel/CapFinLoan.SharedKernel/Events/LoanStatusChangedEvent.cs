namespace CapFinLoan.SharedKernel.Events
{
    /// <summary>
    /// Event published to RabbitMQ when a loan application
    /// status changes. Consumed by NotificationService to
    /// send email notifications to applicants.
    /// </summary>
    public class LoanStatusChangedEvent
    {
        /// <summary>Unique event identifier</summary>
        public Guid EventId { get; set; } = Guid.NewGuid();

        /// <summary>The loan application that changed</summary>
        public Guid ApplicationId { get; set; }

        /// <summary>The applicant's user ID</summary>
        public Guid UserId { get; set; }

        /// <summary>Applicant email address for notification</summary>
        public string ApplicantEmail { get; set; } = string.Empty;

        /// <summary>Applicant full name for personalized message</summary>
        public string ApplicantName { get; set; } = string.Empty;

        /// <summary>Previous status before the change</summary>
        public string OldStatus { get; set; } = string.Empty;

        /// <summary>New status after the change</summary>
        public string NewStatus { get; set; } = string.Empty;

        /// <summary>Admin remarks about the status change</summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>Loan type for context in notification</summary>
        public string LoanType { get; set; } = string.Empty;

        /// <summary>Loan amount for context in notification</summary>
        public decimal LoanAmount { get; set; }

        /// <summary>When this event was created</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
