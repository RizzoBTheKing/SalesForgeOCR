using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IOcrEngine
    {
        Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> ExtractTextPagesAsync(string documentPath, CancellationToken cancellationToken = default);
    }
}
