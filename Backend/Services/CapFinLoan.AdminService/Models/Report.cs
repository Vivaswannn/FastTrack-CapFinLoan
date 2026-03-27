using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapFinLoan.AdminService.Models
{
    /// <summary>
    /// Represents a generated operational report.
    /// Created when admin generates a report from the reports page.
    /// Maps to admin.Reports table in CapFinLoan_Admin database.
    /// </summary>
    [Table("Reports", Schema = "admin")]
    public class Report
    {
        /// <summary>Unique identifier for this report</summary>
        [Key]
        public Guid ReportId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Type of report generated.
        /// Examples: "Summary", "Monthly", "ApplicationsByStatus"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ReportType { get; set; } = string.Empty;

        /// <summary>UTC timestamp when report was generated</summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Email of admin who generated this report</summary>
        [Required]
        [MaxLength(150)]
        public string GeneratedBy { get; set; } = string.Empty;

        /// <summary>
        /// JSON string of filter parameters used to generate this report.
        /// Example: {"startDate":"2025-01-01","endDate":"2025-01-31","status":"All"}
        /// </summary>
        public string? Parameters { get; set; }

        /// <summary>
        /// File path if report was exported to CSV or PDF.
        /// Null if report was only viewed online.
        /// </summary>
        [MaxLength(500)]
        public string? FilePath { get; set; }

        /// <summary>Total number of records in this report</summary>
        public int TotalRecords { get; set; }
    }
}
