using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IThumbnailGenerator
    {
        Task<string> GenerateThumbnailAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default);
    }
}
