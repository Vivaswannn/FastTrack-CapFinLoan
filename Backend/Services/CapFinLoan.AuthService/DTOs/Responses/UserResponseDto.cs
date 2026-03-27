namespace CapFinLoan.AuthService.DTOs.Responses
{
    /// <summary>
    /// Response DTO for user profile and user list endpoints.
    /// Safe to return — never contains PasswordHash.
    /// </summary>
    public class UserResponseDto
    {
        /// <summary>Unique user identifier</summary>
        public Guid UserId { get; set; }

        /// <summary>Full name</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Email address</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Phone number</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>Role: Applicant or Admin</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Whether account is active</summary>
        public bool IsActive { get; set; }

        /// <summary>Account creation timestamp</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Last update timestamp</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
