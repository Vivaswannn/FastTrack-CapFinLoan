using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapFinLoan.AdminService.Models
{
    /// <summary>
    /// Records every admin action for compliance and traceability.
    /// Written automatically by AdminService on every state-changing operation.
    /// </summary>
    [Table("AuditLogs", Schema = "admin")]
    public class AuditLog
    {
        [Key]
        public Guid AuditLogId { get; set; } = Guid.NewGuid();

        /// <summary>The entity type affected (e.g. Decision, Application, Document).</summary>
        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>The ID of the affected entity.</summary>
        [Required]
        public Guid EntityId { get; set; }

        /// <summary>The action performed (e.g. Approved, Rejected, StatusUpdated, DocVerified).</summary>
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Email of the admin who performed the action.</summary>
        [Required]
        [MaxLength(150)]
        public string PerformedBy { get; set; } = string.Empty;

        /// <summary>UserId of the admin who performed the action.</summary>
        public Guid PerformedByUserId { get; set; }

        /// <summary>JSON snapshot of the old state (null for creates).</summary>
        public string? OldValues { get; set; }

        /// <summary>JSON snapshot of the new state.</summary>
        public string? NewValues { get; set; }

        /// <summary>Optional human-readable remarks.</summary>
        [MaxLength(500)]
        public string? Remarks { get; set; }

        /// <summary>Correlation ID from the request for distributed tracing.</summary>
        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
