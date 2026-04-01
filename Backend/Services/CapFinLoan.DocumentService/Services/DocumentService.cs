using CapFinLoan.DocumentService.DTOs.Requests;
using CapFinLoan.DocumentService.DTOs.Responses;
using CapFinLoan.DocumentService.Helpers;
using CapFinLoan.DocumentService.Messaging;
using CapFinLoan.DocumentService.Models;
using CapFinLoan.DocumentService.Repositories.Interfaces;
using CapFinLoan.DocumentService.Services.Interfaces;
using CapFinLoan.SharedKernel.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CapFinLoan.DocumentService.Services
{
    /// <summary>
    /// Implements all document-management business logic.
    /// Validates files, manages disk storage, and enforces access control.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DocumentService> _logger;
        private readonly IMessagePublisher _publisher;

        public DocumentService(
            IDocumentRepository repository,
            IWebHostEnvironment environment,
            ILogger<DocumentService> logger,
            IMessagePublisher publisher)
        {
            _repository  = repository;
            _environment = environment;
            _logger      = logger;
            _publisher   = publisher;
        }

        /// <inheritdoc/>
        public async Task<DocumentResponseDto> UploadDocumentAsync(
            IFormFile file,
            UploadDocumentDto dto,
            Guid userId)
        {
            // 1. Null check
            if (file == null)
                throw new ArgumentException("No file provided.");

            // 2. Extension validation
            if (!FileHelper.IsValidExtension(file.FileName))
                throw new ArgumentException(
                    "Invalid file type. Only PDF, JPG, and PNG are allowed.");

            // 3. Size validation
            if (!FileHelper.IsValidFileSize(file.Length))
                throw new ArgumentException(
                    "File size exceeds 5MB limit.");

            // 4. Mark previous document of same type as replaced
            var existing = await _repository.GetByApplicationIdAndTypeAsync(
                dto.ApplicationId, dto.DocumentType);

            if (existing != null)
            {
                existing.IsReplaced = true;
                await _repository.UpdateAsync(existing);
                _logger.LogInformation(
                    "Previous {DocumentType} replaced for application {ApplicationId}",
                    dto.DocumentType, dto.ApplicationId);
            }

            // 5. Generate unique stored filename
            var storedFileName = FileHelper.GenerateStoredFileName(file.FileName);

            // 6. Determine full path
            var fullPath = FileHelper.GetUploadPath(_environment, storedFileName);

            // 7. Ensure directory exists
            FileHelper.EnsureDirectoryExists(fullPath);

            // 8. Write file to disk
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 9. Build domain record
            var document = new Document
            {
                ApplicationId  = dto.ApplicationId,
                UserId         = userId,
                DocumentType   = dto.DocumentType,
                FileName       = file.FileName,
                StoredFileName = storedFileName,
                FilePath       = FileHelper.GetRelativePath(storedFileName),
                FileExtension  = Path.GetExtension(file.FileName).ToLower(),
                FileSizeBytes  = file.Length,
                IsVerified     = false,
                IsReplaced     = false,
                UploadedAt     = DateTime.UtcNow
            };

            // 10. Persist to database
            var saved = await _repository.CreateAsync(document);

            // 11. Log
            _logger.LogInformation(
                "Document uploaded: {DocumentType} for application {ApplicationId}",
                dto.DocumentType, dto.ApplicationId);

            // 12. Return response
            return MapToResponseDto(saved);
        }

        /// <inheritdoc/>
        public async Task<List<DocumentResponseDto>> GetDocumentsByApplicationAsync(
            Guid applicationId,
            Guid userId,
            string role)
        {
            var documents = await _repository.GetByApplicationIdAsync(applicationId);

            if (role == "Applicant")
            {
                // Applicants can only see their own documents
                documents = documents
                    .Where(d => d.UserId == userId)
                    .ToList();
            }
            // Admin sees all documents for the application

            return documents.Select(MapToResponseDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<DocumentResponseDto> GetDocumentByIdAsync(
            Guid documentId,
            Guid userId,
            string role)
        {
            var document = await _repository.GetByIdAsync(documentId);

            if (document == null)
                throw new KeyNotFoundException("Document not found.");

            if (role == "Applicant" && document.UserId != userId)
                throw new KeyNotFoundException("Document not found or access denied.");

            return MapToResponseDto(document);
        }

        /// <inheritdoc/>
        public async Task<string> GetFilePathAsync(
            Guid documentId,
            Guid userId,
            string role)
        {
            var document = await _repository.GetByIdAsync(documentId);

            if (document == null)
                throw new KeyNotFoundException("Document not found.");

            if (role == "Applicant" && document.UserId != userId)
                throw new KeyNotFoundException("Document not found or access denied.");

            return document.FilePath;
        }

        /// <inheritdoc/>
        public async Task<DocumentResponseDto> VerifyDocumentAsync(
            Guid documentId,
            VerifyDocumentDto dto,
            string adminEmail)
        {
            var document = await _repository.GetByIdAsync(documentId);

            if (document == null)
                throw new KeyNotFoundException("Document not found.");

            document.IsVerified           = dto.IsVerified;
            document.VerifiedBy           = adminEmail;
            document.VerifiedAt           = DateTime.UtcNow;
            document.VerificationRemarks  = dto.VerificationRemarks;

            var updated = await _repository.UpdateAsync(document);

            _logger.LogInformation(
                "Document {DocumentId} verified={IsVerified} by {AdminEmail}",
                documentId, dto.IsVerified, adminEmail);

            // Notify applicant by email when a document is rejected
            if (!dto.IsVerified
                && !string.IsNullOrEmpty(dto.ApplicantEmail)
                && !string.IsNullOrEmpty(dto.ApplicantName))
            {
                await _publisher.PublishLoanStatusChangedAsync(new LoanStatusChangedEvent
                {
                    ApplicationId  = document.ApplicationId,
                    UserId         = document.UserId,
                    ApplicantEmail = dto.ApplicantEmail,
                    ApplicantName  = dto.ApplicantName,
                    OldStatus      = "DocsPending",
                    NewStatus      = "DocumentRejected",
                    Remarks        = dto.VerificationRemarks ?? string.Empty,
                    LoanType       = dto.DocumentType ?? "Document",
                });
            }

            return MapToResponseDto(updated);
        }

        /// <inheritdoc/>
        public async Task<List<DocumentResponseDto>> GetAllDocumentsByApplicationAsync(
            Guid applicationId)
        {
            var documents = await _repository.GetAllByApplicationIdAsync(applicationId);
            return documents.Select(MapToResponseDto).ToList();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Maps a Document domain model to a DocumentResponseDto.
        /// FilePath is intentionally never included in the response.
        /// </summary>
        private static DocumentResponseDto MapToResponseDto(Document doc)
        {
            return new DocumentResponseDto
            {
                DocumentId            = doc.DocumentId,
                ApplicationId         = doc.ApplicationId,
                UserId                = doc.UserId,
                DocumentType          = doc.DocumentType.ToString(),
                FileName              = doc.FileName,
                FileExtension         = doc.FileExtension,
                FileSizeBytes         = doc.FileSizeBytes,
                FileSizeFormatted     = FileHelper.FormatFileSize(doc.FileSizeBytes),
                IsVerified            = doc.IsVerified,
                IsReplaced            = doc.IsReplaced,
                UploadedAt            = doc.UploadedAt,
                VerifiedAt            = doc.VerifiedAt,
                VerifiedBy            = doc.VerifiedBy,
                VerificationRemarks   = doc.VerificationRemarks
            };
        }
    }
}
