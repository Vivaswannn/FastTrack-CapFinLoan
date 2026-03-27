using CapFinLoan.DocumentService.Data;
using CapFinLoan.DocumentService.Models;
using CapFinLoan.DocumentService.Repositories.Interfaces;
using CapFinLoan.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.DocumentService.Repositories
{
    /// <summary>
    /// EF Core implementation of IDocumentRepository.
    /// This is the only layer that accesses DocumentDbContext directly.
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        private readonly DocumentDbContext _context;

        public DocumentRepository(DocumentDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<Document?> GetByIdAsync(Guid documentId)
        {
            return await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
        }

        /// <inheritdoc/>
        public async Task<List<Document>> GetByApplicationIdAsync(Guid applicationId)
        {
            return await _context.Documents
                .AsNoTracking()
                .Where(d => d.ApplicationId == applicationId && !d.IsReplaced)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Document?> GetByApplicationIdAndTypeAsync(
            Guid applicationId,
            DocumentType type)
        {
            return await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.ApplicationId == applicationId
                    && d.DocumentType == type
                    && !d.IsReplaced);
        }

        /// <inheritdoc/>
        public async Task<Document> CreateAsync(Document document)
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        /// <inheritdoc/>
        public async Task<Document> UpdateAsync(Document document)
        {
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
            return document;
        }

        /// <inheritdoc/>
        public async Task<List<Document>> GetAllByApplicationIdAsync(Guid applicationId)
        {
            return await _context.Documents
                .AsNoTracking()
                .Where(d => d.ApplicationId == applicationId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }
    }
}
