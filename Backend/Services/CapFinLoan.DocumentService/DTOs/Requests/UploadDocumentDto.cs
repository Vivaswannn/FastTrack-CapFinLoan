using System.ComponentModel.DataAnnotations;
using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.DocumentService.DTOs.Requests
{
    /// <summary>
    /// Metadata submitted alongside a file upload.
    /// The actual file is received as IFormFile in the controller.
    /// </summary>
    public class UploadDocumentDto
    {
        /// <summary>The loan application this document belongs to.</summary>
        [Required(ErrorMessage = "ApplicationId is required.")]
        public Guid ApplicationId { get; set; }

        /// <summary>The type of KYC document being uploaded.</summary>
        [Required(ErrorMessage = "DocumentType is required.")]
        public DocumentType DocumentType { get; set; }
    }
}
