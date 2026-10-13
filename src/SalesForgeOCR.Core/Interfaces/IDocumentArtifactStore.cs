using SalesForgeOCR.Core.Entities;
namespace SalesForgeOCR.Core.Interfaces;
public interface IDocumentArtifactStore
{
    Task SaveAsync(Guid documentId, Guid versionId, IEnumerable<DocumentText> texts, IEnumerable<DocumentEntity> entities, IEnumerable<DocumentMetadata> metadata, IEnumerable<DocumentImage> images, IEnumerable<DocumentTable> tables, CancellationToken cancellationToken = default);
}
