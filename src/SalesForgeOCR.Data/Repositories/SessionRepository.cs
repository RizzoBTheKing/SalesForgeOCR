using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class SessionRepository
    {
        private readonly string _dbPath;
        public SessionRepository(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public async Task InsertAsync(UserSession session)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO UserSessions (Id, UserId, CreatedAt, ExpiresAt, Machine, IpAddress) VALUES ($id, $user, $created, $expires, $machine, $ip)";
            cmd.Parameters.AddWithValue("$id", session.Id.ToString());
            cmd.Parameters.AddWithValue("$user", session.UserId);
            cmd.Parameters.AddWithValue("$created", session.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$expires", session.ExpiresAt.ToString("o"));
            cmd.Parameters.AddWithValue("$machine", session.Machine ?? string.Empty);
            cmd.Parameters.AddWithValue("$ip", session.IpAddress ?? string.Empty);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InvalidateAsync(Guid sessionId)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM UserSessions WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", sessionId.ToString());
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
