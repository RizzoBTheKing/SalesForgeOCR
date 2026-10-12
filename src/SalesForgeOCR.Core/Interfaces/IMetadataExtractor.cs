using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IMetadataExtractor
    {
        Task<IEnumerable<(string key, string value)>> ExtractMetadataAsync(string documentPath, CancellationToken cancellationToken = default);
    }
}
