using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentTextRepository
    {
        Task AddAsync(DocumentText text, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentText>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task UpdateAsync(DocumentText text, CancellationToken cancellationToken = default);
    }
}
