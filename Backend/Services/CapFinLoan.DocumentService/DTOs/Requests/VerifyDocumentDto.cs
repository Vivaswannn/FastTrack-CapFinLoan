using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.DocumentService.DTOs.Requests
{
    /// <summary>
    /// Request body for admin document verification or rejection.
    /// When IsVerified is false, VerificationRemarks is required.
    /// </summary>
    public class VerifyDocumentDto
    {
        /// <summary>True to verify, false to reject.</summary>
        [Required(ErrorMessage = "IsVerified is required.")]
        public bool IsVerified { get; set; }

        /// <summary>
        /// Admin notes. Required when rejecting (IsVerified=false).
        /// </summary>
        [MaxLength(500, ErrorMessage = "VerificationRemarks must not exceed 500 characters.")]
        public string? VerificationRemarks { get; set; }

        /// <summary>Applicant email — used to send rejection notification.</summary>
        [MaxLength(150)]
        public string? ApplicantEmail { get; set; }

        /// <summary>Applicant full name — used to personalise the email.</summary>
        [MaxLength(100)]
        public string? ApplicantName { get; set; }

        /// <summary>Document type label shown in the email (e.g. "Aadhaar Card").</summary>
        [MaxLength(50)]
        public string? DocumentType { get; set; }
    }
}
