using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Core.Entities;
namespace SalesForgeOCR.DocumentProcessing.Engines;
public class ImageExtractor : IImageExtractor
{
    public async Task<IEnumerable<string>> ExtractImagesAsync(string documentPath, CancellationToken cancellationToken = default)
        => (await ExtractImageDetailsAsync(documentPath,cancellationToken)).Select(image=>image.FilePath);
    public Task<IEnumerable<ExtractedDocumentImage>> ExtractImageDetailsAsync(string documentPath, CancellationToken cancellationToken = default)
        => Task.Run<IEnumerable<ExtractedDocumentImage>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ext=Path.GetExtension(documentPath).ToLowerInvariant();
            if(ext is ".pdf" or ".docx" or ".xlsx")return EmbeddedImageReader.Read(documentPath,cancellationToken);
            return ext is ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" or ".bmp" ? [new(documentPath,1,"Body",0,0,0,0)] : [];
        },cancellationToken);
}
