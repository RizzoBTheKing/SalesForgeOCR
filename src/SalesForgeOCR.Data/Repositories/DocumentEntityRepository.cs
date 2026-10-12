using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Search;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class DocumentEntityRepository : SalesForgeOCR.Core.Interfaces.IDocumentEntityRepository
    {
        private readonly string _dbPath;
        private readonly AuditRepository _audit;
        private readonly SearchIndexService _index;
        public DocumentEntityRepository(string? dbPath = null, AuditRepository? audit = null, SearchIndexService? index = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
            _audit = audit ?? new AuditRepository(_dbPath);
            _index = index ?? new SearchIndexService(_dbPath);
        }

        public async Task AddAsync(DocumentEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DocumentEntities (Id, DocumentId, VersionId, Type, Value, Confidence, PageNumber) VALUES ($id, $doc, $ver, $type, $value, $conf, $page)";
                cmd.Parameters.AddWithValue("$id", entity.Id.ToString());
                cmd.Parameters.AddWithValue("$doc", entity.DocumentId.ToString());
                cmd.Parameters.AddWithValue("$ver", entity.VersionId.ToString());
                cmd.Parameters.AddWithValue("$type", entity.Type);
                cmd.Parameters.AddWithValue("$value", entity.Value);
                cmd.Parameters.AddWithValue("$conf", entity.Confidence);
                cmd.Parameters.AddWithValue("$page", entity.PageNumber);
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                await _index.IndexEntityEntryAsync(entity.DocumentId, entity.Id, entity.Value, 2.0, cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "INSERT INTO EntityFTS (Type, Value) VALUES ($type, $value)";
                fts.Parameters.AddWithValue("$type", entity.Type);
                fts.Parameters.AddWithValue("$value", entity.Value);
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentEntity.Add.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task<IEnumerable<DocumentEntity>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentEntity>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, VersionId, Type, Value, Confidence, PageNumber FROM DocumentEntities WHERE DocumentId = $doc";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentEntity
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    VersionId = Guid.Parse(reader.GetString(2)),
                    Type = reader.GetString(3),
                    Value = reader.GetString(4),
                    Confidence = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5),
                    PageNumber = reader.IsDBNull(6) ? 0 : reader.GetInt32(6)
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
                cmd.CommandText = "DELETE FROM DocumentEntities WHERE DocumentId = $doc";
                cmd.Parameters.AddWithValue("$doc", documentId.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                await _index.RemoveDocumentIndexAsync(documentId, cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "DELETE FROM EntityFTS WHERE rowid IN (SELECT rowid FROM EntityFTS WHERE Value MATCH $val)"; // best-effort
                fts.Parameters.AddWithValue("$val", "*");
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentEntity.Delete.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }
    }
}
