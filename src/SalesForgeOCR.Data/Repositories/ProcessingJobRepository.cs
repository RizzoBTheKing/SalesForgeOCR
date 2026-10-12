using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class ProcessingJobRepository
    {
        private readonly string _dbPath;
        public ProcessingJobRepository(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public async Task InsertAsync(ProcessingJob job)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO ProcessingJobs (Id, DocumentId, VersionId, Status, Attempt, ErrorMessage, CreatedAt, StartedAt, CompletedAt, CreatedBy) VALUES ($id, $doc, $ver, $status, $attempt, $err, $created, $started, $completed, $createdBy)";
            cmd.Parameters.AddWithValue("$id", job.Id.ToString());
            cmd.Parameters.AddWithValue("$doc", job.DocumentId.ToString());
            cmd.Parameters.AddWithValue("$ver", job.VersionId.ToString());
            cmd.Parameters.AddWithValue("$status", (int)job.Status);
            cmd.Parameters.AddWithValue("$attempt", job.Attempt);
            cmd.Parameters.AddWithValue("$err", job.ErrorMessage ?? string.Empty);
            cmd.Parameters.AddWithValue("$created", job.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$started", job.StartedAt?.ToString("o") ?? string.Empty);
            cmd.Parameters.AddWithValue("$completed", job.CompletedAt?.ToString("o") ?? string.Empty);
            cmd.Parameters.AddWithValue("$createdBy", job.CreatedBy ?? string.Empty);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
