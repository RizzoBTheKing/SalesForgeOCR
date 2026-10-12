using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentImageRepository
    {
        Task AddAsync(DocumentImage image, CancellationToken cancellationToken = default);
        Task<DocumentImage?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentImage>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task UpdateAsync(DocumentImage image, CancellationToken cancellationToken = default);
    }
}
