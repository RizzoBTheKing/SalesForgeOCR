using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Data.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Infrastructure.Configuration
{
    public class SettingsService : ISettingsService
    {
        private readonly SettingsRepository _repo;
        public SettingsService(SettingsRepository repo)
        {
            _repo = repo;
        }

        public Task<ApplicationSetting?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            return _repo.GetAsync(key);
        }

        public Task SetAsync(ApplicationSetting setting, CancellationToken cancellationToken = default)
        {
            return _repo.SetAsync(setting);
        }

        public Task<IEnumerable<ApplicationSetting>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _repo.GetAllAsync();
        }
    }
}
