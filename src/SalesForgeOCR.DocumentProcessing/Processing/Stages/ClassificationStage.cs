using SalesForgeOCR.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class ClassificationStage : ProcessingStage
    {
        private readonly ITemplateClassifier _classifier;
        public ClassificationStage(ITemplateClassifier classifier)
        {
            _classifier = classifier;
        }

        public override string Name => "Classification";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            var allText = string.Join("\n", context.ExtractedText.Select(t => t.Content));
            var label = await _classifier.ClassifyAsync(allText, cancellationToken);
            if (!string.IsNullOrWhiteSpace(label))
            {
                context.Metadata.Add(new SalesForgeOCR.Core.Entities.DocumentMetadata
                {
                    DocumentId = context.Document.Id,
                    Key = "Template",
                    Value = label
                });
            }
            return ProcessingResult.Ok();
        }
    }
}
