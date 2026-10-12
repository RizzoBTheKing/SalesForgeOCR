using SalesForgeOCR.Core.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> CreateDocumentAsync(string fileName, long size, string createdBy, CancellationToken cancellationToken = default);
        Task AddVersionAsync(Guid documentId, DocumentVersion version, CancellationToken cancellationToken = default);
        Task<Document?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
