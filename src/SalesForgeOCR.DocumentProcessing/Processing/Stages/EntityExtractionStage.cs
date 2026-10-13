using SalesForgeOCR.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class EntityExtractionStage : ProcessingStage
    {
        private readonly IEntityExtractor _extractor;
        public EntityExtractionStage(IEntityExtractor extractor)
        {
            _extractor = extractor;
        }

        public override string Name => "EntityExtraction";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            foreach (var text in context.ExtractedText)
            {
                var entities = await _extractor.ExtractEntitiesAsync(text.Content, cancellationToken);
                foreach (var (type, value, confidence, page) in entities)
                {
                    context.Entities.Add(new SalesForgeOCR.Core.Entities.DocumentEntity
                    {
                        DocumentId = context.Document.Id,
                        VersionId = context.Version.Id,
                        Type = type,
                        Value = value,
                        Confidence = confidence,
                        PageNumber = text.PageNumber
                    });
                }
            }
            return ProcessingResult.Ok();
        }
    }
}
