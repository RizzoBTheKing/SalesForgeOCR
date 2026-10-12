using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class SqliteDocumentRepository : IDocumentRepository
    {
        private readonly string _dbPath;

        public SqliteDocumentRepository(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // DatabaseInitializer handles creation; ensure file exists
            await Task.CompletedTask;
        }

        public async Task AddDocumentAsync(Document doc, CancellationToken cancellationToken = default)
        {
            var connectionString = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Documents (Id, FileName, Size, CreatedAt, CurrentVersionId) VALUES ($id, $file, $size, $created, $current)";
            cmd.Parameters.AddWithValue("$id", doc.Id.ToString());
            cmd.Parameters.AddWithValue("$file", doc.FileName);
            cmd.Parameters.AddWithValue("$size", doc.Size);
            cmd.Parameters.AddWithValue("$created", doc.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$current", doc.CurrentVersionId?.ToString() ?? string.Empty);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<Document?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, FileName, Size, CreatedAt, CurrentVersionId FROM Documents WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new Document
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    FileName = reader.GetString(1),
                    Size = reader.GetInt64(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    CurrentVersionId = string.IsNullOrWhiteSpace(reader.GetString(4)) ? (Guid?)null : Guid.Parse(reader.GetString(4))
                };
            }
            return null;
        }

        public async Task<IEnumerable<Document>> SearchDocumentsAsync(string query, CancellationToken cancellationToken = default)
        {
            var results = new List<Document>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT d.Id, d.FileName, d.Size, d.CreatedAt, d.CurrentVersionId FROM Documents d JOIN DocumentTextFTS f ON f.DocumentId = d.Id WHERE f.Content MATCH $q LIMIT 100";
            cmd.Parameters.AddWithValue("$q", query + "*");
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new Document
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    FileName = reader.GetString(1),
                    Size = reader.GetInt64(2),
                    CreatedAt = DateTime.Parse(reader.GetString(3)),
                    CurrentVersionId = string.IsNullOrWhiteSpace(reader.GetString(4)) ? (Guid?)null : Guid.Parse(reader.GetString(4))
                });
            }
            return results;
        }

        public async Task AddVersionAsync(DocumentVersion version, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO DocumentVersions (Id, DocumentId, VersionNumber, FileName, Size, CreatedAt, CreatedBy) VALUES ($id, $doc, $ver, $file, $size, $created, $createdBy)";
            cmd.Parameters.AddWithValue("$id", version.Id.ToString());
            cmd.Parameters.AddWithValue("$doc", version.DocumentId.ToString());
            cmd.Parameters.AddWithValue("$ver", version.VersionNumber);
            cmd.Parameters.AddWithValue("$file", version.FileName);
            cmd.Parameters.AddWithValue("$size", version.Size);
            cmd.Parameters.AddWithValue("$created", version.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("$createdBy", version.CreatedBy ?? string.Empty);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            // Update Documents.CurrentVersionId
            var upd = conn.CreateCommand();
            upd.CommandText = "UPDATE Documents SET CurrentVersionId = $vid WHERE Id = $did";
            upd.Parameters.AddWithValue("$vid", version.Id.ToString());
            upd.Parameters.AddWithValue("$did", version.DocumentId.ToString());
            await upd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IEnumerable<DocumentVersion>> GetVersionsAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentVersion>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, VersionNumber, FileName, Size, CreatedAt, CreatedBy FROM DocumentVersions WHERE DocumentId = $did ORDER BY VersionNumber DESC";
            cmd.Parameters.AddWithValue("$did", documentId.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentVersion
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    VersionNumber = reader.GetInt32(2),
                    FileName = reader.GetString(3),
                    Size = reader.GetInt64(4),
                    CreatedAt = DateTime.Parse(reader.GetString(5)),
                    CreatedBy = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
                });
            }
            return list;
        }
    }
}
