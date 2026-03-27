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
    }
}
