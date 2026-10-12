using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentRepository
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task AddDocumentAsync(Document doc, CancellationToken cancellationToken = default);
        Task<Document?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Document>> SearchDocumentsAsync(string query, CancellationToken cancellationToken = default);
        Task AddVersionAsync(DocumentVersion version, CancellationToken cancellationToken = default);
        Task<IEnumerable<DocumentVersion>> GetVersionsAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
