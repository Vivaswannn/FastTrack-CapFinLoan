using CapFinLoan.AdminService.Models;
using CapFinLoan.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.AdminService.Data
{
    /// <summary>
    /// Database context for AdminService.
    /// Manages admin.Decisions and admin.Reports tables
    /// in CapFinLoan_Admin database.
    /// </summary>
    public class AdminDbContext : DbContext
    {
        public AdminDbContext(
            DbContextOptions<AdminDbContext> options)
            : base(options)
        {
        }

        /// <summary>Decisions table — all admin approve/reject decisions</summary>
        public DbSet<Decision> Decisions { get; set; }

        /// <summary>Reports table — all generated operational reports</summary>
        public DbSet<Report> Reports { get; set; }

        /// <summary>Audit log table — every admin action for compliance</summary>
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Store DecisionType enum as string
            modelBuilder.Entity<Decision>()
                .Property(d => d.DecisionType)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Index for fast decision lookup by application
            modelBuilder.Entity<Decision>()
                .HasIndex(d => d.ApplicationId)
                .IsUnique()
                .HasDatabaseName("IX_Decisions_ApplicationId");
            // Unique — one decision per application

            // Index for fast lookup by user
            modelBuilder.Entity<Decision>()
                .HasIndex(d => d.UserId)
                .HasDatabaseName("IX_Decisions_UserId");

            // Index for reports by date
            modelBuilder.Entity<Report>()
                .HasIndex(r => r.GeneratedAt)
                .HasDatabaseName("IX_Reports_GeneratedAt");

            // Audit log indexes for fast lookup
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.EntityId)
                .HasDatabaseName("IX_AuditLogs_EntityId");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt)
                .HasDatabaseName("IX_AuditLogs_CreatedAt");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.PerformedByUserId)
                .HasDatabaseName("IX_AuditLogs_PerformedByUserId");
        }
    }
}
