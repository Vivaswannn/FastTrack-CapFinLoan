using CapFinLoan.DocumentService.Models;
using CapFinLoan.SharedKernel.Enums;

namespace CapFinLoan.DocumentService.Repositories.Interfaces
{
    /// <summary>
    /// Data-access interface for the Document entity.
    /// All methods are asynchronous.
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>Returns a document by its primary key, or null if not found.</summary>
        Task<Document?> GetByIdAsync(Guid documentId);

        /// <summary>
        /// Returns all active (non-replaced) documents for a given application,
        /// ordered by upload date descending.
        /// </summary>
        Task<List<Document>> GetByApplicationIdAsync(Guid applicationId);

        /// <summary>
        /// Returns the active (non-replaced) document of a specific type
        /// for a given application, or null if none exists.
        /// Used to detect duplicate uploads before creating a new record.
        /// </summary>
        Task<Document?> GetByApplicationIdAndTypeAsync(Guid applicationId, DocumentType type);

        /// <summary>Persists a new document record and returns it.</summary>
        Task<Document> CreateAsync(Document document);

        /// <summary>Saves changes to an existing document record and returns it.</summary>
        Task<Document> UpdateAsync(Document document);

        /// <summary>
        /// Returns ALL documents (including replaced ones) for an application.
        /// Intended for admin audit views.
        /// </summary>
        Task<List<Document>> GetAllByApplicationIdAsync(Guid applicationId);
    }
}
