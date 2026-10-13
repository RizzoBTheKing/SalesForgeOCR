using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Core.Entities;
namespace SalesForgeOCR.DocumentProcessing.Processing.Stages;
public class TextExtractionStage : ProcessingStage
{
    private readonly IOcrEngine _ocr;
    public TextExtractionStage(IOcrEngine ocr) => _ocr = ocr;
    public override string Name => "TextExtraction";
    public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
    {
        var pages = await _ocr.ExtractTextPagesAsync(context.RenderedPdfPath ?? context.Version.StoragePath ?? context.Version.FileName, cancellationToken);
        var number = 1;
        foreach (var content in pages)
            context.ExtractedText.Add(new DocumentText { DocumentId = context.Document.Id, VersionId = context.Version.Id, PageNumber = number++, Content = content, Confidence = 0 });
        if (context.ExtractedText.All(t => string.IsNullOrWhiteSpace(t.Content)))
            return ProcessingResult.Fail("No readable text was found. Check the document quality and language.");
        return ProcessingResult.Ok();
    }
}
