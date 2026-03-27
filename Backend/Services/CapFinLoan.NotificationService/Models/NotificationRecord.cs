namespace CapFinLoan.NotificationService.Models
{
    /// <summary>
    /// Represents a notification that was sent or attempted.
    /// In production this would be stored in a database.
    /// For training we keep it in memory and log it.
    /// </summary>
    public class NotificationRecord
    {
        public Guid NotificationId { get; set; }
            = Guid.NewGuid();
        public Guid ApplicationId { get; set; }
        public string RecipientEmail { get; set; }
            = string.Empty;
        public string RecipientName { get; set; }
            = string.Empty;
        public string Subject { get; set; }
            = string.Empty;
        public string Body { get; set; }
            = string.Empty;
        public bool IsSuccess { get; set; }
        public DateTime SentAt { get; set; }
            = DateTime.UtcNow;
        public string? ErrorMessage { get; set; }
    }
}
