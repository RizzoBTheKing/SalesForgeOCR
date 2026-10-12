using SalesForgeOCR.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IAuditRepository
    {
        Task InsertAsync(AuditLog entry, CancellationToken cancellationToken = default);
    }
}
