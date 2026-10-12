using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IEntityExtractor
    {
        Task<IEnumerable<(string type, string value, double confidence, int page)>> ExtractEntitiesAsync(string text, CancellationToken cancellationToken = default);
    }
}
