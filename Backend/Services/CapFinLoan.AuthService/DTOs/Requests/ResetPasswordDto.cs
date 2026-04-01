using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.AuthService.DTOs.Requests
{
    /// <summary>
    /// Request DTO for resetting the password using a verified OTP.
    /// </summary>
    public class ResetPasswordDto
    {
        /// <summary>
        /// The registered email address of the account.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The 6-digit OTP sent to the email.
        /// </summary>
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OtpCode { get; set; } = string.Empty;

        /// <summary>
        /// The new password to set for the account.
        /// </summary>
        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
