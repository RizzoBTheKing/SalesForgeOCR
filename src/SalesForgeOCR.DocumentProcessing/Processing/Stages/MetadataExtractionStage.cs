using SalesForgeOCR.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class MetadataExtractionStage : ProcessingStage
    {
        private readonly IMetadataExtractor _extractor;
        public MetadataExtractionStage(IMetadataExtractor extractor)
        {
            _extractor = extractor;
        }

        public override string Name => "MetadataExtraction";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            var meta = await _extractor.ExtractMetadataAsync(context.Version.StoragePath ?? context.Version.FileName, cancellationToken);
            foreach (var (key, value) in meta)
            {
                context.Metadata.Add(new SalesForgeOCR.Core.Entities.DocumentMetadata
                {
                    DocumentId = context.Document.Id,
                    Key = key,
                    Value = value
                });
            }
            return ProcessingResult.Ok();
        }
    }
}
