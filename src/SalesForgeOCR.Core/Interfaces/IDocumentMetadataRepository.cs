using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentMetadataRepository
    {
        Task AddAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentMetadata>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task UpdateAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default);
    }
}
