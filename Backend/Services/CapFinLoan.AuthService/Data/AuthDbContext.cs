using CapFinLoan.AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.AuthService.Data
{
    /// <summary>
    /// Database context for AuthService.
    /// Manages auth.Users table in CapFinLoan_Auth database.
    /// This context only knows about Users — nothing else.
    /// </summary>
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options)
            : base(options)
        {
        }

        /// <summary>Users table — all system users</summary>
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Email must be unique — no duplicate accounts
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            // Seed one admin user so system works from day one
            // Password is "Admin@1234" — hashed with BCrypt
            modelBuilder.Entity<User>().HasData(new User
            {
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FullName = "System Admin",
                Email = "admin@capfinloan.com",
                PasswordHash = "$2a$11$sSgwsvIunYMs0wYal/0y6.09HHvD1GdYn9rPjH7OJDNPzTJQJDMgm",
                Phone = "9999999999",
                Role = "Admin",
                IsActive = true,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = null
            });
        }
    }
}
