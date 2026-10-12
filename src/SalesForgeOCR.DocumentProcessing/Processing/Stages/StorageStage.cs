using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Search;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class StorageStage : ProcessingStage
    {
        private readonly IDocumentRepository _repo;
        private readonly IDocumentTextRepository _textRepo;
        private readonly IDocumentEntityRepository _entityRepo;
        private readonly IDocumentMetadataRepository _metadataRepo;
        private readonly IDocumentImageRepository _imageRepo;
        private readonly SearchIndexService _indexService;

        public StorageStage(IDocumentRepository repo,
            IDocumentTextRepository textRepo,
            IDocumentEntityRepository entityRepo,
            IDocumentMetadataRepository metadataRepo,
            IDocumentImageRepository imageRepo,
            SearchIndexService indexService)
        {
            _repo = repo;
            _textRepo = textRepo;
            _entityRepo = entityRepo;
            _metadataRepo = metadataRepo;
            _imageRepo = imageRepo;
            _indexService = indexService;
        }

        public override string Name => "Storage";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            // Persist document version
            await _repo.AddVersionAsync(context.Document.Id, context.Version, cancellationToken);

            // Persist texts
            foreach (var t in context.ExtractedText)
            {
                await _textRepo.AddAsync(t, cancellationToken);
                await _indexService.IndexTextEntryAsync(t.DocumentId, t.Id, t.Content, 1.0, cancellationToken);
            }

            // Persist entities
            foreach (var e in context.Entities)
            {
                await _entityRepo.AddAsync(e, cancellationToken);
                // Indexing handled in repository
            }

            // Persist metadata
            foreach (var m in context.Metadata)
            {
                await _metadataRepo.AddAsync(m, cancellationToken);
                // Indexing handled in repository
            }

            // Persist images and locations
            foreach (var img in context.Images)
            {
                await _imageRepo.AddAsync(img, cancellationToken);
                await _indexService.IndexMetadataEntryAsync(img.DocumentId, img.Id, img.FileName, 0.2, cancellationToken);
            }

            return ProcessingResult.Ok();
        }
    }
}
