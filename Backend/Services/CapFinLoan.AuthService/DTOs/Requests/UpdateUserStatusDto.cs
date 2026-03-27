using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.AuthService.DTOs.Requests
{
    /// <summary>
    /// Request DTO for admin to activate or deactivate a user account
    /// </summary>
    public class UpdateUserStatusDto
    {
        /// <summary>True to activate, False to deactivate</summary>
        [Required]
        public bool IsActive { get; set; }

        /// <summary>Reason for status change</summary>
        public string? Reason { get; set; }
    }
}
