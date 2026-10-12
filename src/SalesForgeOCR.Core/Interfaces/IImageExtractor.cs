using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IImageExtractor
    {
        Task<IEnumerable<string>> ExtractImagesAsync(string documentPath, CancellationToken cancellationToken = default);
    }
}
