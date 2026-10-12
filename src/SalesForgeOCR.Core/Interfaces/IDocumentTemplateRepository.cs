using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentTemplateRepository
    {
        Task AddAsync(DocumentTemplate template, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<DocumentTemplate?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
