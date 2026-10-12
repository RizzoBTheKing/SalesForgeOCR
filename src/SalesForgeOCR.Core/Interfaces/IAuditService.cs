using SalesForgeOCR.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(AuditLog entry, CancellationToken cancellationToken = default);
    }
}
