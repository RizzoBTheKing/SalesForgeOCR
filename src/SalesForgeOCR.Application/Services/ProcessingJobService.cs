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
        private readonly IDocumentRepository _docRepo;
        private readonly SalesForgeOCR.DocumentProcessing.Processing.DocumentProcessingJob _processor;

        public ProcessingJobService(IProcessingJobRepository repo, IAuditService audit, IDocumentRepository docRepo, SalesForgeOCR.DocumentProcessing.Processing.DocumentProcessingJob processor)
        {
            _repo = repo;
            _audit = audit;
            _docRepo = docRepo;
            _processor = processor;
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

        public async Task EnqueueJobAsync(Guid jobId, IJobQueue jobQueue, CancellationToken cancellationToken = default)
        {
            // Load the job to get full details
            var job = await _repo.GetByIdAsync(jobId, cancellationToken);
            if (job == null) return;

            // Add job to queue
            await jobQueue.EnqueueAsync(async ct =>
            {
                try
                {
                    // Update job status to InProgress
                    job.MarkInProgress();
                    await _repo.UpdateAsync(job, ct);

                    // Get the document and version details
                    var document = await _docRepo.GetDocumentAsync(job.DocumentId, ct);
                    var versions = await _docRepo.GetVersionsAsync(job.DocumentId, ct);
                    var version = versions.FirstOrDefault(v => v.Id == job.VersionId);

                    if (document == null || version == null)
                    {
                        job.MarkFailed("Document or Version not found");
                        await _repo.UpdateAsync(job, ct);
                        return;
                    }

                    // Get the processing pipeline and execute
                    var processingJob = _processor;
                    if (processingJob != null)
                    {
                        var result = await processingJob.RunAsync(document, version, ct);

                        // Update job status to Completed
                        if (!result.Success) throw new InvalidOperationException(result.Message);
                        job.MarkCompleted();
                        await _repo.UpdateAsync(job, ct);
                    }
                }
                catch (Exception ex)
                {
                    // Update job status to Failed
                    job.MarkFailed(ex.Message);
                    await _repo.UpdateAsync(job, CancellationToken.None);
                    throw; // Re-throw so it's handled by the background worker
                }
            });
        }
    }
}
