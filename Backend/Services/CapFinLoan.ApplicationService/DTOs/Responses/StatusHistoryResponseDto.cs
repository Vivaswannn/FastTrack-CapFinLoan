namespace CapFinLoan.ApplicationService.DTOs.Responses
{
    /// <summary>
    /// Response DTO representing a single status history entry in the audit trail.
    /// </summary>
    public class StatusHistoryResponseDto
    {
        /// <summary>Unique identifier of this history record.</summary>
        public Guid HistoryId { get; set; }

        /// <summary>The application this history belongs to.</summary>
        public Guid ApplicationId { get; set; }

        /// <summary>Status before this change (as string).</summary>
        public string FromStatus { get; set; } = string.Empty;

        /// <summary>Status after this change (as string).</summary>
        public string ToStatus { get; set; } = string.Empty;

        /// <summary>Reason or note for this status change.</summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>Email of the person who made this status change.</summary>
        public string ChangedBy { get; set; } = string.Empty;

        /// <summary>UTC timestamp of this status change.</summary>
        public DateTime ChangedAt { get; set; }
    }
}
