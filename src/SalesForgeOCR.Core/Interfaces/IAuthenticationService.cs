using SalesForgeOCR.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Task<bool> VerifyMasterPasswordAsync(string password, CancellationToken cancellationToken = default);
        Task SetupMasterPasswordAsync(string password, CancellationToken cancellationToken = default);
        Task<UserSession> CreateSessionAsync(string userId, int hours = 8, CancellationToken cancellationToken = default);
        Task InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    }
}
