using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _repo;
        public DocumentService(IDocumentRepository repo)
        {
            _repo = repo;
        }

        public async Task<Document> CreateDocumentAsync(string fileName, long size, string createdBy, CancellationToken cancellationToken = default)
        {
            var doc = new Document
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                Size = size,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddDocumentAsync(doc, cancellationToken);
            return doc;
        }

        public Task AddVersionAsync(Guid documentId, DocumentVersion version, CancellationToken cancellationToken = default)
        {
            return _repo.AddVersionAsync(version, cancellationToken);
        }

        public Task<Document?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _repo.GetDocumentAsync(id, cancellationToken);
        }
    }
}
