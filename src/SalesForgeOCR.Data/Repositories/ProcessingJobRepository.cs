using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class ProcessingJobRepository : SalesForgeOCR.Core.Interfaces.IProcessingJobRepository
    {
        private readonly string _dbPath;
        public ProcessingJobRepository(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public Task InsertAsync(ProcessingJob job) => AddAsync(job);

        public async Task AddAsync(ProcessingJob job, System.Threading.CancellationToken cancellationToken = default)
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

        public async Task<ProcessingJob?> GetByIdAsync(Guid id, System.Threading.CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, VersionId, Status, Attempt, ErrorMessage, CreatedAt, StartedAt, CompletedAt, CreatedBy FROM ProcessingJobs WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new ProcessingJob
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    VersionId = Guid.Parse(reader.GetString(2)),
                    Status = (ProcessingStatus)reader.GetInt32(3),
                    Attempt = reader.GetInt32(4),
                    ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6)),
                    StartedAt = (reader.IsDBNull(7) || string.IsNullOrEmpty(reader.GetString(7))) ? (DateTime?)null : DateTime.Parse(reader.GetString(7)),
                    CompletedAt = (reader.IsDBNull(8) || string.IsNullOrEmpty(reader.GetString(8))) ? (DateTime?)null : DateTime.Parse(reader.GetString(8)),
                    CreatedBy = reader.IsDBNull(9) ? string.Empty : reader.GetString(9)
                };
            }
            return null;
        }

        public async Task UpdateAsync(ProcessingJob job, System.Threading.CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE ProcessingJobs SET Status = $status, Attempt = $attempt, ErrorMessage = $err, StartedAt = $started, CompletedAt = $completed WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", job.Id.ToString());
            cmd.Parameters.AddWithValue("$status", (int)job.Status);
            cmd.Parameters.AddWithValue("$attempt", job.Attempt);
            cmd.Parameters.AddWithValue("$err", job.ErrorMessage ?? string.Empty);
            cmd.Parameters.AddWithValue("$started", job.StartedAt?.ToString("o") ?? string.Empty);
            cmd.Parameters.AddWithValue("$completed", job.CompletedAt?.ToString("o") ?? string.Empty);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        public async Task<System.Collections.Generic.IEnumerable<ProcessingJob>> GetByDocumentAsync(Guid documentId, System.Threading.CancellationToken cancellationToken = default)
        {
            var jobs = new System.Collections.Generic.List<ProcessingJob>();
            await using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString());
            await conn.OpenAsync(cancellationToken);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM ProcessingJobs WHERE DocumentId = $doc ORDER BY CreatedAt DESC";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            var ids = new System.Collections.Generic.List<Guid>();
            await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
                while (await reader.ReadAsync(cancellationToken)) ids.Add(Guid.Parse(reader.GetString(0)));
            foreach (var id in ids)
            {
                var job = await GetByIdAsync(id, cancellationToken);
                if (job != null) jobs.Add(job);
            }
            return jobs;
        }
        public async Task RecoverInterruptedAsync()
        {
            await using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString());
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE ProcessingJobs SET Status = 3, ErrorMessage = 'Application closed before processing completed. Import the document again.', CompletedAt = $now WHERE Status IN (0, 1)";
            cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
