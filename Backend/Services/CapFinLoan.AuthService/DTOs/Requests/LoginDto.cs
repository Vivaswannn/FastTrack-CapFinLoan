using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.AuthService.DTOs.Requests
{
    /// <summary>
    /// Request DTO for user login.
    /// Validated by LoginValidator before processing.
    /// </summary>
    public class LoginDto
    {
        /// <summary>Registered email address</summary>
        [Required]
        public string Email { get; set; } = string.Empty;

        /// <summary>Account password</summary>
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
