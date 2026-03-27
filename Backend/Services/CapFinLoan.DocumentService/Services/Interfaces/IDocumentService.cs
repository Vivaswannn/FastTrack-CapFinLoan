using CapFinLoan.DocumentService.DTOs.Requests;
using CapFinLoan.DocumentService.DTOs.Responses;
using Microsoft.AspNetCore.Http;

namespace CapFinLoan.DocumentService.Services.Interfaces
{
    /// <summary>
    /// Business-logic interface for document management.
    /// Handles upload, retrieval, download, and admin verification.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Validates and saves an uploaded file, then creates the database record.
        /// Marks any previous document of the same type as replaced.
        /// </summary>
        Task<DocumentResponseDto> UploadDocumentAsync(
            IFormFile file,
            UploadDocumentDto dto,
            Guid userId);

        /// <summary>
        /// Returns active documents for an application.
        /// Applicants may only see their own documents;
        /// Admins may see any application's documents.
        /// </summary>
        Task<List<DocumentResponseDto>> GetDocumentsByApplicationAsync(
            Guid applicationId,
            Guid userId,
            string role);

        /// <summary>
        /// Returns a single document by its identifier.
        /// Applicants may only retrieve their own documents.
        /// </summary>
        Task<DocumentResponseDto> GetDocumentByIdAsync(
            Guid documentId,
            Guid userId,
            string role);

        /// <summary>
        /// Returns the server-side file path for streaming the file to the client.
        /// Access is restricted to the document owner (applicant) or any admin.
        /// </summary>
        Task<string> GetFilePathAsync(
            Guid documentId,
            Guid userId,
            string role);

        /// <summary>
        /// Marks a document as verified or rejected.
        /// Admin-only operation.
        /// </summary>
        Task<DocumentResponseDto> VerifyDocumentAsync(
            Guid documentId,
            VerifyDocumentDto dto,
            string adminEmail);

        /// <summary>
        /// Returns ALL documents (including replaced) for an application.
        /// Admin-only operation for audit purposes.
        /// </summary>
        Task<List<DocumentResponseDto>> GetAllDocumentsByApplicationAsync(
            Guid applicationId);
    }
}
