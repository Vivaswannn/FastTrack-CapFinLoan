using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.DocumentService.Models
{
    /// <summary>
    /// Represents a KYC or income document uploaded by an applicant.
    /// Maps to docs.Documents table in CapFinLoan_Document database.
    /// Files are stored on disk in wwwroot/uploads/ folder.
    /// This record stores the metadata about the file.
    /// </summary>
    [Table("Documents", Schema = "docs")]
    public class Document
    {
        /// <summary>Unique identifier for this document record</summary>
        [Key]
        public Guid DocumentId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The loan application this document belongs to.
        /// Cross-service reference — NOT a foreign key.
        /// DocumentService does not have direct DB access to
        /// ApplicationService database.
        /// </summary>
        [Required]
        public Guid ApplicationId { get; set; }

        /// <summary>
        /// The user who uploaded this document.
        /// Cross-service reference — NOT a foreign key.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>Type of document uploaded</summary>
        [Required]
        public DocumentType DocumentType { get; set; }

        /// <summary>
        /// Original filename as uploaded by user.
        /// Example: "aadhar_front.pdf"
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Full path where file is stored on disk.
        /// Example: "wwwroot/uploads/2025/01/abc123.pdf"
        /// Never expose this path directly in API responses.
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// A unique name generated for the file on disk.
        /// Prevents overwriting when two users upload same filename.
        /// Example: "3f2504e0-4f89-11d3-9a0c.pdf"
        /// </summary>
        [MaxLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>File extension in lowercase. Example: ".pdf"</summary>
        [MaxLength(10)]
        public string FileExtension { get; set; } = string.Empty;

        /// <summary>File size in bytes. Used to enforce 5MB limit.</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Whether admin has verified this document as genuine.
        /// Default is false — unverified on upload.
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>UTC timestamp when file was uploaded</summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp when admin verified the document</summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>Email of admin who verified this document</summary>
        [MaxLength(150)]
        public string? VerifiedBy { get; set; }

        /// <summary>
        /// Admin notes on verification.
        /// Used to explain rejection or request re-upload.
        /// </summary>
        [MaxLength(500)]
        public string? VerificationRemarks { get; set; }

        /// <summary>Whether this document has been replaced by a newer upload</summary>
        public bool IsReplaced { get; set; } = false;
    }
}
