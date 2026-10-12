using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Data.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Infrastructure.Audit
{
    public class AuditService : IAuditService
    {
        private readonly AuditRepository _repo;
        public AuditService(AuditRepository repo)
        {
            _repo = repo;
        }

        public async Task LogAsync(AuditLog entry, CancellationToken cancellationToken = default)
        {
            try
            {
                await _repo.InsertAsync(entry, cancellationToken);
            }
            catch (Exception)
            {
                // swallow - auditing should not crash processing
            }
        }
    }
}
