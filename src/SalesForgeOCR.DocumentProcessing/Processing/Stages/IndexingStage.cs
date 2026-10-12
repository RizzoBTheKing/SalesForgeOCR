using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Search;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class IndexingStage : ProcessingStage
    {
        private readonly ISearchEngine _searchEngine;
        private readonly SearchIndexService _indexService;
        public IndexingStage(ISearchEngine searchEngine, SearchIndexService indexService)
        {
            _searchEngine = searchEngine;
            _indexService = indexService;
        }

        public override string Name => "Indexing";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            var pages = context.ExtractedText.Select(t => (t.PageNumber, t.Content));
            await _searchEngine.IndexDocumentTextAsync(context.Document.Id, context.Version.Id, pages, cancellationToken);

            // Persisted entities and metadata should already have stable IDs; ensure index is synchronized
            var entities = context.Entities.Select(e => (e.Id, e.Type, e.Value, e.PageNumber));
            foreach (var e in entities)
            {
                await _indexService.IndexEntityEntryAsync(context.Document.Id, e.Id, e.Value, 2.0, cancellationToken);
            }

            var metadata = context.Metadata.Select(m => (m.Id, m.Key, m.Value));
            foreach (var m in metadata)
            {
                await _indexService.IndexMetadataEntryAsync(context.Document.Id, m.Id, m.Value, 0.5, cancellationToken);
            }

            return ProcessingResult.Ok();
        }
    }
}
