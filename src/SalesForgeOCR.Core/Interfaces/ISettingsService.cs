using SalesForgeOCR.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface ISettingsService
    {
        Task<ApplicationSetting?> GetAsync(string key, CancellationToken cancellationToken = default);
        Task SetAsync(ApplicationSetting setting, CancellationToken cancellationToken = default);
        Task<IEnumerable<ApplicationSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
