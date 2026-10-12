using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class ProcessingJobService
    {
        private readonly IProcessingJobRepository _repo;
        private readonly IAuditService _audit;

        public ProcessingJobService(IProcessingJobRepository repo, IAuditService audit)
        {
            _repo = repo;
            _audit = audit;
        }

        public async Task CreateAsync(ProcessingJob job, CancellationToken cancellationToken = default)
        {
            await _repo.AddAsync(job, cancellationToken);
            await _audit.LogAsync(new AuditLog { EventType = "ProcessingJob.Create", Actor = job.CreatedBy, Details = $"Created job {job.Id} for document {job.DocumentId}" }, cancellationToken);
        }

        public Task UpdateAsync(ProcessingJob job, CancellationToken cancellationToken = default)
        {
            return _repo.UpdateAsync(job, cancellationToken);
        }

        public Task<IEnumerable<ProcessingJob>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return _repo.GetByDocumentAsync(documentId, cancellationToken);
        }
    }
}
