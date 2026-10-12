using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class AuditRepository : SalesForgeOCR.Core.Interfaces.IAuditRepository
    {
        private readonly string _dbPath;
        public AuditRepository(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public async Task InsertAsync(SalesForgeOCR.Core.Entities.AuditLog entry, System.Threading.CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO AuditLogs (Id, Timestamp, EventType, Actor, Details, Severity) VALUES ($id, $ts, $etype, $actor, $details, $severity)";
            cmd.Parameters.AddWithValue("$id", entry.Id.ToString());
            cmd.Parameters.AddWithValue("$ts", entry.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("$etype", entry.EventType);
            cmd.Parameters.AddWithValue("$actor", entry.Actor ?? string.Empty);
            cmd.Parameters.AddWithValue("$details", entry.Details ?? string.Empty);
            cmd.Parameters.AddWithValue("$severity", entry.Severity ?? "Information");
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
