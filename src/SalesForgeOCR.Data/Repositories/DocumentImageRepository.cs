using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class DocumentImageRepository : SalesForgeOCR.Core.Interfaces.IDocumentImageRepository
    {
        private readonly string _dbPath;
        private readonly AuditRepository _audit;
        public DocumentImageRepository(string? dbPath = null, AuditRepository? audit = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
            _audit = audit ?? new AuditRepository(_dbPath);
        }

        public async Task AddAsync(DocumentImage image, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DocumentImages (Id, DocumentId, VersionId, PageNumber, FilePath, ThumbnailPath, ContentType, ExtractedAt, Hash) VALUES ($id, $doc, $ver, $page, $file, $thumb, $contentType, $extracted, $hash)";
                cmd.Parameters.AddWithValue("$id", image.Id.ToString());
                cmd.Parameters.AddWithValue("$doc", image.DocumentId.ToString());
                cmd.Parameters.AddWithValue("$ver", image.VersionId.ToString());
                cmd.Parameters.AddWithValue("$page", image.PageNumber);
                cmd.Parameters.AddWithValue("$file", image.FilePath);
                cmd.Parameters.AddWithValue("$thumb", image.ThumbnailPath ?? string.Empty);
                cmd.Parameters.AddWithValue("$contentType", image.ContentType ?? string.Empty);
                cmd.Parameters.AddWithValue("$extracted", image.ExtractedAt.ToString("o"));
                cmd.Parameters.AddWithValue("$hash", image.Hash ?? string.Empty);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentImage.Add.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task<DocumentImage?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, VersionId, PageNumber, FilePath, ThumbnailPath, ContentType, ExtractedAt, Hash FROM DocumentImages WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new DocumentImage
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    VersionId = Guid.Parse(reader.GetString(2)),
                    PageNumber = reader.GetInt32(3),
                    FilePath = reader.GetString(4),
                    ThumbnailPath = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    ContentType = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    ExtractedAt = reader.IsDBNull(7) ? DateTime.UtcNow : DateTime.Parse(reader.GetString(7)),
                    Hash = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                };
            }
            return null;
        }

        public async Task<IEnumerable<DocumentImage>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentImage>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, VersionId, PageNumber, FilePath, ThumbnailPath, ContentType, ExtractedAt, Hash FROM DocumentImages WHERE DocumentId = $doc ORDER BY PageNumber";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentImage
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    VersionId = Guid.Parse(reader.GetString(2)),
                    PageNumber = reader.GetInt32(3),
                    FilePath = reader.GetString(4),
                    ThumbnailPath = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    ContentType = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    ExtractedAt = reader.IsDBNull(7) ? DateTime.UtcNow : DateTime.Parse(reader.GetString(7)),
                    Hash = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                });
            }
            return list;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DocumentImages WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentImage.Delete.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task UpdateAsync(DocumentImage image, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE DocumentImages SET ThumbnailPath = $thumb, ContentType = $contentType, Hash = $hash WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", image.Id.ToString());
                cmd.Parameters.AddWithValue("$thumb", image.ThumbnailPath ?? string.Empty);
                cmd.Parameters.AddWithValue("$contentType", image.ContentType ?? string.Empty);
                cmd.Parameters.AddWithValue("$hash", image.Hash ?? string.Empty);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentImage.Update.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }
    }
}
