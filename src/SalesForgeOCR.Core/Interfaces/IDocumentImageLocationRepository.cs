using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentImageLocationRepository
    {
        Task AddAsync(DocumentImageLocation loc, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentImageLocation>> GetByImageAsync(Guid imageId, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentImageLocation>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
        Task DeleteByImageAsync(Guid imageId, CancellationToken cancellationToken = default);
    }
}
