using CapFinLoan.AdminService.Data;
using CapFinLoan.AdminService.Models;
using CapFinLoan.AdminService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.AdminService.Services
{
    /// <summary>
    /// Writes every admin action to admin.AuditLogs for compliance and traceability.
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly AdminDbContext _db;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            AdminDbContext db,
            ILogger<AuditLogService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task LogAsync(
            string entityType,
            Guid entityId,
            string action,
            string performedBy,
            Guid performedByUserId,
            string? oldValues = null,
            string? newValues = null,
            string? remarks = null,
            string? correlationId = null)
        {
            try
            {
                var entry = new AuditLog
                {
                    EntityType        = entityType,
                    EntityId          = entityId,
                    Action            = action,
                    PerformedBy       = performedBy,
                    PerformedByUserId = performedByUserId,
                    OldValues         = oldValues,
                    NewValues         = newValues,
                    Remarks           = remarks,
                    CorrelationId     = correlationId,
                    CreatedAt         = DateTime.UtcNow
                };

                _db.AuditLogs.Add(entry);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Audit: {Action} on {EntityType} {EntityId} by {PerformedBy}",
                    action, entityType, entityId, performedBy);
            }
            catch (Exception ex)
            {
                // Audit logging must never crash the main operation
                _logger.LogError(ex,
                    "Failed to write audit log for {Action} on {EntityType} {EntityId}",
                    action, entityType, entityId);
            }
        }

        /// <inheritdoc/>
        public async Task<List<AuditLogDto>> GetAuditTrailAsync(Guid entityId)
        {
            var logs = await _db.AuditLogs
                .Where(a => a.EntityId == entityId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return logs.Select(a => new AuditLogDto(
                a.AuditLogId,
                a.EntityType,
                a.EntityId,
                a.Action,
                a.PerformedBy,
                a.OldValues,
                a.NewValues,
                a.Remarks,
                a.CorrelationId,
                a.CreatedAt)).ToList();
        }
    }
}
