using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IImageExtractor
    {
        async Task<IEnumerable<SalesForgeOCR.Core.Entities.ExtractedDocumentImage>> ExtractImageDetailsAsync(string documentPath, CancellationToken cancellationToken = default)
        {
            var paths = await ExtractImagesAsync(documentPath, cancellationToken);
            return paths.Select(path => new SalesForgeOCR.Core.Entities.ExtractedDocumentImage(path,1,"Body",0,0,0,0));
        }
        Task<IEnumerable<string>> ExtractImagesAsync(string documentPath, CancellationToken cancellationToken = default);
    }
}
