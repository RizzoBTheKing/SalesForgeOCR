using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Data.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Security
{
    public class AuthenticationService : IAuthenticationService
    {
        private const string MasterKey = "__MASTER_PASSWORD__";
        private readonly SettingsRepository _settings;
        private readonly SessionRepository _sessions;
        private readonly AuditRepository _audit;

        public AuthenticationService(SettingsRepository settings, SessionRepository sessions, AuditRepository audit)
        {
            _settings = settings;
            _sessions = sessions;
            _audit = audit;
        }

        public async Task<bool> VerifyMasterPasswordAsync(string password, CancellationToken cancellationToken = default)
        {
            var setting = await _settings.GetAsync(MasterKey);
            if (setting == null) return false;
            var ok = PasswordHasher.Verify(password, setting.Value);
            await _audit.InsertAsync(new AuditLog { EventType = ok ? "Auth.Success" : "Auth.Failure", Actor = "system", Details = ok ? "Master password verified" : "Master password failed" });
            return ok;
        }

        public async Task SetupMasterPasswordAsync(string password, CancellationToken cancellationToken = default)
        {
            var hashed = PasswordHasher.Hash(password);
            var setting = new ApplicationSetting { Key = MasterKey, Value = hashed, Description = "Master password hash", UpdatedAt = DateTime.UtcNow };
            await _settings.SetAsync(setting);
            await _audit.InsertAsync(new AuditLog { EventType = "Auth.Setup", Actor = "system", Details = "Master password setup" });
        }

        public async Task<UserSession> CreateSessionAsync(string userId, int hours = 8, CancellationToken cancellationToken = default)
        {
            var session = new UserSession { UserId = userId, ExpiresAt = DateTime.UtcNow.AddHours(hours) };
            await _sessions.InsertAsync(session);
            await _audit.InsertAsync(new AuditLog { EventType = "Session.Create", Actor = userId, Details = $"Session {session.Id} created" });
            return session;
        }

        public async Task InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (Guid.TryParse(sessionId, out var gid))
            {
                await _sessions.InvalidateAsync(gid);
                await _audit.InsertAsync(new AuditLog { EventType = "Session.Invalidate", Actor = "system", Details = $"Session {sessionId} invalidated" });
            }
        }
    }
}
