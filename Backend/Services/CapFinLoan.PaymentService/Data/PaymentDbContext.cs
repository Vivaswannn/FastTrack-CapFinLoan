using CapFinLoan.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.PaymentService.Data
{
    /// <summary>EF Core DbContext for the PaymentService database.</summary>
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
            : base(options) { }

        /// <summary>Payment disbursement records.</summary>
        public DbSet<Payment> Payments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.PaymentId);
                entity.HasIndex(p => p.ApplicationId);
                entity.HasIndex(p => p.UserId);
                entity.Property(p => p.Status).HasMaxLength(20);
            });
        }
    }
}
