using CapFinLoan.ApplicationService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.ApplicationService.Data
{
    /// <summary>
    /// Database context for ApplicationService.
    /// Manages core.LoanApplications and core.StatusHistory tables
    /// in CapFinLoan_Loan database.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>Loan applications table</summary>
        public DbSet<LoanApplication> LoanApplications { get; set; }

        /// <summary>Status change audit trail table</summary>
        public DbSet<StatusHistory> StatusHistories { get; set; }

        /// <summary>Reliable messaging Outbox pattern</summary>
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── LoanApplication configuration ────────────────

            // Store Status enum as string for database readability
            // Database shows "Draft" not "0"
            modelBuilder.Entity<LoanApplication>()
                .Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Store LoanType enum as string
            modelBuilder.Entity<LoanApplication>()
                .Property(a => a.LoanType)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Index on UserId for fast "my applications" queries
            modelBuilder.Entity<LoanApplication>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_LoanApplications_UserId");

            // Index on Status for fast queue filtering
            modelBuilder.Entity<LoanApplication>()
                .HasIndex(a => a.Status)
                .HasDatabaseName("IX_LoanApplications_Status");

            // Decimal precision for money fields
            modelBuilder.Entity<LoanApplication>()
                .Property(a => a.LoanAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<LoanApplication>()
                .Property(a => a.MonthlyIncome)
                .HasPrecision(18, 2);

            // ── StatusHistory configuration ──────────────────

            // Store enum values as strings
            modelBuilder.Entity<StatusHistory>()
                .Property(h => h.FromStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            modelBuilder.Entity<StatusHistory>()
                .Property(h => h.ToStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            // One application has many history records
            // Deleting application deletes all its history
            modelBuilder.Entity<LoanApplication>()
                .HasMany(a => a.StatusHistories)
                .WithOne(h => h.LoanApplication)
                .HasForeignKey(h => h.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for fast history lookup by application
            modelBuilder.Entity<StatusHistory>()
                .HasIndex(h => h.ApplicationId)
                .HasDatabaseName("IX_StatusHistory_ApplicationId");
        }
    }
}
