using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Search;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class DocumentMetadataRepository : SalesForgeOCR.Core.Interfaces.IDocumentMetadataRepository
    {
        private readonly string _dbPath;
        private readonly AuditRepository _audit;
        private readonly SearchIndexService _index;
        public DocumentMetadataRepository(string? dbPath = null, AuditRepository? audit = null, SearchIndexService? index = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
            _audit = audit ?? new AuditRepository(_dbPath);
            _index = index ?? new SearchIndexService(_dbPath);
        }

        public async Task AddAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DocumentMetadata (Id, DocumentId, Key, Value) VALUES ($id, $doc, $key, $value)";
                cmd.Parameters.AddWithValue("$id", metadata.Id.ToString());
                cmd.Parameters.AddWithValue("$doc", metadata.DocumentId.ToString());
                cmd.Parameters.AddWithValue("$key", metadata.Key);
                cmd.Parameters.AddWithValue("$value", metadata.Value ?? string.Empty);
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                await _index.IndexMetadataEntryAsync(metadata.DocumentId, metadata.Id, metadata.Value ?? string.Empty, 0.5, cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "INSERT INTO MetadataFTS (Key, Value) VALUES ($key, $value)";
                fts.Parameters.AddWithValue("$key", metadata.Key);
                fts.Parameters.AddWithValue("$value", metadata.Value ?? string.Empty);
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentMetadata.Add.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task<IEnumerable<DocumentMetadata>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentMetadata>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, Key, Value FROM DocumentMetadata WHERE DocumentId = $doc";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentMetadata
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    Key = reader.GetString(2),
                    Value = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                });
            }
            return list;
        }

        public async Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DocumentMetadata WHERE DocumentId = $doc";
                cmd.Parameters.AddWithValue("$doc", documentId.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                using var indexDelete = conn.CreateCommand();
                indexDelete.CommandText = "DELETE FROM SearchIndexFTS WHERE DocumentId = $doc AND SourceType = 'Metadata'";
                indexDelete.Parameters.AddWithValue("$doc", documentId.ToString());
                await indexDelete.ExecuteNonQueryAsync(cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "DELETE FROM MetadataFTS; INSERT INTO MetadataFTS (Key, Value) SELECT Key, Value FROM DocumentMetadata";
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentMetadata.Delete.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task UpdateAsync(DocumentMetadata metadata, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE DocumentMetadata SET Value = $value WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", metadata.Id.ToString());
                cmd.Parameters.AddWithValue("$value", metadata.Value ?? string.Empty);
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                await _index.UpdateIndexEntryAsync(metadata.Id, metadata.Value ?? string.Empty, 0.5, cancellationToken);

                var upd = conn.CreateCommand();
                upd.CommandText = "INSERT INTO MetadataFTS (Key, Value) VALUES ($key, $value)";
                upd.Parameters.AddWithValue("$key", metadata.Key);
                upd.Parameters.AddWithValue("$value", metadata.Value ?? string.Empty);
                await upd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentMetadata.Update.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }
    }
}
