using SalesForgeOCR.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Security
{
    public class MasterPasswordSetupService
    {
        private readonly IAuthenticationService _auth;
        public MasterPasswordSetupService(IAuthenticationService auth)
        {
            _auth = auth;
        }

        public Task SetupAsync(string password, CancellationToken cancellationToken = default)
        {
            return _auth.SetupMasterPasswordAsync(password, cancellationToken);
        }
    }
}
