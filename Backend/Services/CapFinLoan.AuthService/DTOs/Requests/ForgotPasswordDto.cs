using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.AuthService.DTOs.Requests
{
    /// <summary>
    /// Request DTO for initiating the forgot password flow.
    /// Triggers an OTP to be sent to the registered email.
    /// </summary>
    public class ForgotPasswordDto
    {
        /// <summary>
        /// The registered email address of the account.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }
}
