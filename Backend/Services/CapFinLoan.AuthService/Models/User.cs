using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CapFinLoan.AuthService.Models
{
    /// <summary>
    /// Represents a system user — either an Applicant or Admin.
    /// Maps to auth.Users table in CapFinLoan_Auth database.
    /// </summary>
    [Table("Users", Schema = "auth")]
    public class User
    {
        /// <summary>Unique identifier for the user</summary>
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        /// <summary>Full legal name of the user</summary>
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Email address — used as login username.
        /// Must be unique across all users.
        /// </summary>
        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt hashed password.
        /// Never store plain text password here.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Mobile phone number</summary>
        [MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Role of the user in the system.
        /// Allowed values: "Applicant" or "Admin"
        /// Default is Applicant on registration.
        /// Admin can only be created via database seed.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Applicant";

        /// <summary>
        /// Whether this account is active.
        /// Inactive users cannot login.
        /// Admin can activate/deactivate accounts.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>UTC timestamp when account was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of last profile update</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Expected OTP code if MFA is pending</summary>
        [MaxLength(6)]
        public string? OtpCode { get; set; }

        /// <summary>OTP Expiration time in UTC</summary>
        public DateTime? OtpExpiry { get; set; }
    }
}
