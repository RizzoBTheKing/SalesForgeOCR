using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface ISearchEngine
    {
        Task IndexDocumentTextAsync(Guid documentId, Guid versionId, IEnumerable<(int page, string content)> pages, CancellationToken cancellationToken = default);
        Task IndexEntitiesAsync(Guid documentId, IEnumerable<(string type, string value)> entities, CancellationToken cancellationToken = default);
        Task IndexMetadataAsync(Guid documentId, IEnumerable<(string key, string value)> metadata, CancellationToken cancellationToken = default);
        Task<IEnumerable<(Guid documentId, double score)>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
        Task<IEnumerable<(Guid documentId, double score)>> SearchEntitiesAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
        Task<IEnumerable<(Guid documentId, double score)>> SearchMetadataAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
    }
}
