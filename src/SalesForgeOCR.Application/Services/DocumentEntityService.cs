using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class DocumentEntityService
    {
        private readonly IDocumentEntityRepository _repo;
        private readonly IAuditService _audit;
        public DocumentEntityService(IDocumentEntityRepository repo, IAuditService audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task AddAsync(DocumentEntity entity, CancellationToken cancellationToken = default)
        {
            entity.Validate();
            await _repo.AddAsync(entity, cancellationToken);
            await _audit.LogAsync(new AuditLog { EventType = "DocumentEntity.Add", Actor = "system", Details = $"Added entity {entity.Type} to document {entity.DocumentId}" }, cancellationToken);
        }

        public Task<IEnumerable<DocumentEntity>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repo.GetByDocumentAsync(documentId, cancellationToken);
        public Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repo.DeleteByDocumentAsync(documentId, cancellationToken);
    }
}
