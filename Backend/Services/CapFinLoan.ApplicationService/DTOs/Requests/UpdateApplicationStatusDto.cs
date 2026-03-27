using CapFinLoan.SharedKernel.Enums;
using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.ApplicationService.DTOs.Requests
{
    /// <summary>
    /// Request DTO for updating the status of a loan application.
    /// Used exclusively by Admin endpoints.
    /// </summary>
    public class UpdateApplicationStatusDto
    {
        /// <summary>The new status to transition the application to.</summary>
        [Required]
        public ApplicationStatus NewStatus { get; set; }

        /// <summary>
        /// Reason for the status change.
        /// Required when NewStatus is Rejected.
        /// </summary>
        [MaxLength(500)]
        public string Remarks { get; set; } = string.Empty;
    }
}
