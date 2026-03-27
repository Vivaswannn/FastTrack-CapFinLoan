using CapFinLoan.DocumentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.DocumentService.Data
{
    /// <summary>
    /// Database context for DocumentService.
    /// Manages docs.Documents table in CapFinLoan_Document database.
    /// </summary>
    public class DocumentDbContext : DbContext
    {
        public DocumentDbContext(
            DbContextOptions<DocumentDbContext> options)
            : base(options)
        {
        }

        /// <summary>Documents table — all uploaded files metadata</summary>
        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Store DocumentType enum as string
            modelBuilder.Entity<Document>()
                .Property(d => d.DocumentType)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Index for fast lookup by application
            modelBuilder.Entity<Document>()
                .HasIndex(d => d.ApplicationId)
                .HasDatabaseName("IX_Documents_ApplicationId");

            // Index for fast lookup by user
            modelBuilder.Entity<Document>()
                .HasIndex(d => d.UserId)
                .HasDatabaseName("IX_Documents_UserId");

            // Composite index for getting docs by app + type
            modelBuilder.Entity<Document>()
                .HasIndex(d => new { d.ApplicationId, d.DocumentType })
                .HasDatabaseName("IX_Documents_ApplicationId_DocumentType");
        }
    }
}
