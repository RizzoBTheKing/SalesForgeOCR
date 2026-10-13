using SalesForgeOCR.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class ImageExtractionStage : ProcessingStage
    {
        private readonly IImageExtractor _imageExtractor;
        public ImageExtractionStage(IImageExtractor imageExtractor)
        {
            _imageExtractor = imageExtractor;
        }

        public override string Name => "ImageExtraction";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            var images = await _imageExtractor.ExtractImageDetailsAsync(context.RenderedPdfPath ?? context.Version.StoragePath ?? context.Version.FileName, cancellationToken);
            foreach (var path in images)
            {
                context.Images.Add(new SalesForgeOCR.Core.Entities.DocumentImage
                {
                    DocumentId = context.Document.Id,
                    VersionId = context.Version.Id,
                    PageNumber = path.PageNumber,
                    Role = path.Role,
                    FilePath = path.FilePath
                });
            }
            return ProcessingResult.Ok();
        }
    }
}
