using System;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface ITemplateClassifier
    {
        Task<string?> ClassifyAsync(string documentText, CancellationToken cancellationToken = default);
    }
}
