using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class DocumentTemplateService
    {
        private readonly IDocumentTemplateRepository _repo;
        private readonly IAuditService _audit;
        public DocumentTemplateService(IDocumentTemplateRepository repo, IAuditService audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task AddAsync(DocumentTemplate template, CancellationToken cancellationToken = default)
        {
            template.Validate();
            await _repo.AddAsync(template, cancellationToken);
            await _audit.LogAsync(new AuditLog { EventType = "DocumentTemplate.Add", Actor = "system", Details = $"Added template {template.Name}" }, cancellationToken);
        }

        public Task<IEnumerable<DocumentTemplate>> GetAllAsync(CancellationToken cancellationToken = default) => _repo.GetAllAsync(cancellationToken);
        public Task<DocumentTemplate?> GetAsync(Guid id, CancellationToken cancellationToken = default) => _repo.GetAsync(id, cancellationToken);
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => _repo.DeleteAsync(id, cancellationToken);
    }
}
