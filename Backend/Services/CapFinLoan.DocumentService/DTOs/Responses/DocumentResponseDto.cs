namespace CapFinLoan.DocumentService.DTOs.Responses
{
    /// <summary>
    /// Response DTO for document metadata.
    /// FilePath is intentionally excluded for security.
    /// </summary>
    public class DocumentResponseDto
    {
        /// <summary>Unique document identifier.</summary>
        public Guid DocumentId { get; set; }

        /// <summary>The loan application this document belongs to.</summary>
        public Guid ApplicationId { get; set; }

        /// <summary>The user who uploaded this document.</summary>
        public Guid UserId { get; set; }

        /// <summary>Document type as string (e.g. "AadhaarCard").</summary>
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>Original filename as uploaded by the user.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>File extension in lowercase. Example: ".pdf"</summary>
        public string FileExtension { get; set; } = string.Empty;

        /// <summary>File size in bytes.</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>Human-readable file size. Example: "2.4 MB"</summary>
        public string FileSizeFormatted { get; set; } = string.Empty;

        /// <summary>Whether this document has been verified by an admin.</summary>
        public bool IsVerified { get; set; }

        /// <summary>Whether this document has been superseded by a newer upload.</summary>
        public bool IsReplaced { get; set; }

        /// <summary>UTC timestamp when the file was uploaded.</summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>UTC timestamp when admin verified the document.</summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>Email of the admin who verified the document.</summary>
        public string? VerifiedBy { get; set; }

        /// <summary>Admin notes on verification or rejection.</summary>
        public string? VerificationRemarks { get; set; }
    }
}
