using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Data.Repositories;
using System;
using System.Threading.Tasks;

namespace SalesForgeOCR.Security
{
    public class SessionManager
    {
        private readonly SessionRepository _sessions;
        public SessionManager(SessionRepository sessions)
        {
            _sessions = sessions;
        }

        public Task CreateAsync(UserSession session)
        {
            return _sessions.InsertAsync(session);
        }

        public Task InvalidateAsync(Guid sessionId)
        {
            return _sessions.InvalidateAsync(sessionId);
        }
    }
}
