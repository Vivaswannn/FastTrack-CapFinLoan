namespace CapFinLoan.AdminService.Services.Interfaces
{
    /// <summary>
    /// Records admin actions to the audit log for compliance and traceability.
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Logs an admin action asynchronously. Never throws — failures are swallowed
        /// and logged so audit logging never blocks the main operation.
        /// </summary>
        Task LogAsync(
            string entityType,
            Guid entityId,
            string action,
            string performedBy,
            Guid performedByUserId,
            string? oldValues = null,
            string? newValues = null,
            string? remarks = null,
            string? correlationId = null);

        /// <summary>Returns the full audit trail for a specific entity.</summary>
        Task<List<AuditLogDto>> GetAuditTrailAsync(Guid entityId);
    }

    public record AuditLogDto(
        Guid AuditLogId,
        string EntityType,
        Guid EntityId,
        string Action,
        string PerformedBy,
        string? OldValues,
        string? NewValues,
        string? Remarks,
        string? CorrelationId,
        DateTime CreatedAt);
}
