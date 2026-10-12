using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface ITableExtractor
    {
        Task<IEnumerable<(int page, string html)>> ExtractTablesAsync(string documentPath, CancellationToken cancellationToken = default);
    }
}
