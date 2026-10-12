using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class DocumentMetadataService
    {
        private readonly IDocumentMetadataRepository _repo;
        private readonly IAuditService _audit;
        public DocumentMetadataService(IDocumentMetadataRepository repo, IAuditService audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task AddAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default)
        {
            metadata.Validate();
            await _repo.AddAsync(metadata, cancellationToken);
            await _audit.LogAsync(new AuditLog { EventType = "DocumentMetadata.Add", Actor = "system", Details = $"Added metadata {metadata.Key} for document {metadata.DocumentId}" }, cancellationToken);
        }

        public Task<IEnumerable<DocumentMetadata>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repo.GetByDocumentAsync(documentId, cancellationToken);

        public Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repo.DeleteByDocumentAsync(documentId, cancellationToken);

        public Task UpdateAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default) => _repo.UpdateAsync(metadata, cancellationToken);
    }
}
