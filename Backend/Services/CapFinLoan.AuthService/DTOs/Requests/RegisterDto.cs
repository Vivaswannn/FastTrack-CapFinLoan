using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.AuthService.DTOs.Requests
{
    /// <summary>
    /// Request DTO for applicant registration.
    /// Validated by RegisterValidator before processing.
    /// </summary>
    public class RegisterDto
    {
        /// <summary>Full legal name of the applicant</summary>
        [Required]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Email address — used as login username</summary>
        [Required]
        public string Email { get; set; } = string.Empty;

        /// <summary>Mobile phone number</summary>
        [Required]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Plain text password — will be BCrypt hashed in service.
        /// Never stored as plain text.
        /// </summary>
        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>Password confirmation — must match Password</summary>
        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
