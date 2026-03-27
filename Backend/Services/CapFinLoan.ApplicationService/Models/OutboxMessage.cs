namespace CapFinLoan.ApplicationService.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// For example "LoanSubmittedEvent"
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// Serialized JSON payload of the event
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public string? Error { get; set; }
}
