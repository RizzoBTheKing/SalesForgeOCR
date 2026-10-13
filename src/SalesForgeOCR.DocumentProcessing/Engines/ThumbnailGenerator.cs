using System.Drawing;
using System.Drawing.Imaging;
using SalesForgeOCR.Core.Interfaces;
namespace SalesForgeOCR.DocumentProcessing.Engines;
public class ThumbnailGenerator : IThumbnailGenerator
{
    public Task<string> GenerateThumbnailAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        using var source = Image.FromFile(imagePath);
        var scale = Math.Min((double)width / source.Width, (double)height / source.Height);
        using var bitmap = new Bitmap(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, 0, 0, bitmap.Width, bitmap.Height);
        var directory = Path.Combine(Path.GetDirectoryName(imagePath)!, "thumbnails");
        Directory.CreateDirectory(directory);
        var output = Path.Combine(directory, $"{Path.GetFileName(imagePath)}.{width}x{height}.png");
        bitmap.Save(output, ImageFormat.Png);
        return Task.FromResult(output);
    }
}
