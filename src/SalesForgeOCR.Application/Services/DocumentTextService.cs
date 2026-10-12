using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Search;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class DocumentTextService
    {
        private readonly IDocumentTextRepository _repo;
        private readonly IAuditService _audit;
        private readonly SearchIndexService _index;

        public DocumentTextService(IDocumentTextRepository repo, IAuditService audit, SearchIndexService index)
        {
            _repo = repo;
            _audit = audit;
            _index = index;
        }

        public async Task AddTextAsync(DocumentText text, CancellationToken cancellationToken = default)
        {
            text.Validate();
            await _repo.AddAsync(text, cancellationToken);
            await _index.IndexTextEntryAsync(text.DocumentId, text.Id, text.Content, 1.0, cancellationToken);
            await _audit.LogAsync(new AuditLog { EventType = "DocumentText.Add", Actor = "system", Details = $"Added text for document {text.DocumentId} page {text.PageNumber}" }, cancellationToken);
        }

        public Task<IEnumerable<DocumentText>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return _repo.GetByDocumentAsync(documentId, cancellationToken);
        }

        public async Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            await _repo.DeleteByDocumentAsync(documentId, cancellationToken);
            await _index.RemoveDocumentIndexAsync(documentId, cancellationToken);
            await _audit.LogAsync(new AuditLog { EventType = "DocumentText.Delete", Actor = "system", Details = $"Deleted text for document {documentId}" }, cancellationToken);
        }

        public async Task UpdateAsync(DocumentText text, CancellationToken cancellationToken = default)
        {
            await _repo.UpdateAsync(text, cancellationToken);
            await _index.UpdateIndexEntryAsync(text.Id, text.Content, 1.0, cancellationToken);
            await _audit.LogAsync(new AuditLog { EventType = "DocumentText.Update", Actor = "system", Details = $"Updated text {text.Id}" }, cancellationToken);
        }
    }
}
