using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class DocumentTableService
    {
        private readonly IDocumentTableRepository _repo;
        private readonly IAuditService _audit;
        public DocumentTableService(IDocumentTableRepository repo, IAuditService audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task AddAsync(DocumentTable table, CancellationToken cancellationToken = default)
        {
            table.Validate();
            await _repo.AddAsync(table, cancellationToken);
            await _audit.LogAsync(new AuditLog { EventType = "DocumentTable.Add", Actor = "system", Details = $"Added table for document {table.DocumentId}" }, cancellationToken);
        }

        public Task<IEnumerable<DocumentTable>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repo.GetByDocumentAsync(documentId, cancellationToken);
        public Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repo.DeleteByDocumentAsync(documentId, cancellationToken);
    }
}
