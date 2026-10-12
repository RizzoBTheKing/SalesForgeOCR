using SalesForgeOCR.Core.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class TextExtractionStage : ProcessingStage
    {
        private readonly IOcrEngine _ocr;
        public TextExtractionStage(IOcrEngine ocr)
        {
            _ocr = ocr;
        }

        public override string Name => "TextExtraction";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            // For simplicity: If document is an image or scanned PDF, OCR will be used by caller
            // If document already has text, downstream systems will handle it. Here call OCR for each image.
            foreach (var img in context.Images.ToList())
            {
                var text = await _ocr.ExtractTextAsync(img.FilePath, cancellationToken);
                context.ExtractedText.Add(new SalesForgeOCR.Core.Entities.DocumentText
                {
                    DocumentId = context.Document.Id,
                    VersionId = context.Version.Id,
                    PageNumber = img.PageNumber,
                    Content = text,
                    Confidence = 1.0
                });
            }

            return ProcessingResult.Ok();
        }
    }
}
