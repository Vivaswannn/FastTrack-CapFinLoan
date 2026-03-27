namespace CapFinLoan.AuthService.DTOs.Responses
{
    /// <summary>
    /// Response DTO returned after successful login or registration.
    /// Contains JWT token and user details needed by React frontend.
    /// Never contains PasswordHash.
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>JWT Bearer token for API authentication</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Token expiry time in UTC</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Unique user identifier</summary>
        public Guid UserId { get; set; }

        /// <summary>Full name of the user</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Email address of the user</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Role of the user: Applicant or Admin.
        /// React uses this to redirect to correct dashboard.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>True if MFA OTP is required</summary>
        public bool RequiresOtp { get; set; } = false;
    }
}
