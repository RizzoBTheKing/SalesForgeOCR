using System;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IDocumentProcessor
    {
        Task ProcessAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
