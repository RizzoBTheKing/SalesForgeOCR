using SalesForgeOCR.Core.Interfaces;
namespace SalesForgeOCR.DocumentProcessing.Engines;
public class ImageExtractor : IImageExtractor
{
    public Task<IEnumerable<string>> ExtractImagesAsync(string documentPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ext = Path.GetExtension(documentPath).ToLowerInvariant();
        IEnumerable<string> paths = ext is ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" or ".bmp" ? [documentPath] : [];
        return Task.FromResult(paths);
    }
}
