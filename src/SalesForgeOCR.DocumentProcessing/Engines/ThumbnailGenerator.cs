using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Engines
{
    public class ThumbnailGenerator : SalesForgeOCR.Core.Interfaces.IThumbnailGenerator
    {
        public async Task<string> GenerateThumbnailAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default)
        {
            var dir = Path.Combine(Path.GetDirectoryName(imagePath) ?? Path.GetTempPath(), "thumbnails");
            Directory.CreateDirectory(dir);
            var outPath = Path.Combine(dir, $"thumb_{Path.GetFileName(imagePath)}");

            // If thumbnail exists and source hash not changed, return existing
            if (File.Exists(outPath))
            {
                return outPath;
            }

            using var image = await Image.LoadAsync(imagePath, cancellationToken);
            image.Mutate(x => x.Resize(new ResizeOptions { Size = new SixLabors.ImageSharp.Size(width, height), Mode = ResizeMode.Max }));
            await image.SaveAsync(outPath, cancellationToken);
            return outPath;
        }
    }
}
