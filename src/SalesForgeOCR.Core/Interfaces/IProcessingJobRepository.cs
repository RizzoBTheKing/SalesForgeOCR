using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IProcessingJobRepository
    {
        Task AddAsync(ProcessingJob job, CancellationToken cancellationToken = default);
        Task UpdateAsync(ProcessingJob job, CancellationToken cancellationToken = default);
        Task<IEnumerable<ProcessingJob>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
