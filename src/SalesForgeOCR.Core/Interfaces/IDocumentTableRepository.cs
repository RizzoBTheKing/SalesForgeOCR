using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentTableRepository
    {
        Task AddAsync(DocumentTable table, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentTable>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
