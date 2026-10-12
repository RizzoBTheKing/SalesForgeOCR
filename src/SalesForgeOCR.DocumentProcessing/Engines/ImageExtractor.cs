using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig; 
using System.Linq;

namespace SalesForgeOCR.DocumentProcessing.Engines
{
    public class ImageExtractor : SalesForgeOCR.Core.Interfaces.IImageExtractor
    {
        public async Task<IEnumerable<string>> ExtractImagesAsync(string documentPath, CancellationToken cancellationToken = default)
        {
            var ext = Path.GetExtension(documentPath)?.ToLowerInvariant();
            if (ext == ".pdf")
            {
                var result = new List<string>();
                using var doc = PdfDocument.Open(documentPath);
                var pageIndex = 1;
                foreach (var page in doc.GetPages())
                {
                    var images = page.GetImages();
                    foreach (var img in images)
                    {
                        var id = Guid.NewGuid().ToString();
                        var outPath = Path.Combine(Path.GetTempPath(), $"sfocr_img_{id}.png");
                        try
                        {
                            using var ms = new MemoryStream(img.RawBytes);
                            await File.WriteAllBytesAsync(outPath, ms.ToArray(), cancellationToken);
                            result.Add(outPath);
                        }
                        catch
                        {
                            // skip
                        }
                    }
                    pageIndex++;
                }
                return result;
            }

            // For image files, return the path itself
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tif" || ext == ".tiff" || ext == ".bmp")
            {
                return new[] { documentPath };
            }

            return Array.Empty<string>();
        }
    }
}
