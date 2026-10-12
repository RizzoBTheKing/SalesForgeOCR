using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentEntityRepository
    {
        Task AddAsync(DocumentEntity entity, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentEntity>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
