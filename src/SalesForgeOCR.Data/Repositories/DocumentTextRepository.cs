using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class DocumentTextRepository : SalesForgeOCR.Core.Interfaces.IDocumentTextRepository
    {
        private readonly string _dbPath;
        private readonly AuditRepository _audit;
        public DocumentTextRepository(string? dbPath = null, AuditRepository? audit = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
            _audit = audit ?? new AuditRepository(_dbPath);
        }

        public async Task AddAsync(DocumentText text, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DocumentText (Id, DocumentId, VersionId, PageNumber, Content, Confidence) VALUES ($id, $doc, $ver, $page, $content, $conf)";
                cmd.Parameters.AddWithValue("$id", text.Id.ToString());
                cmd.Parameters.AddWithValue("$doc", text.DocumentId.ToString());
                cmd.Parameters.AddWithValue("$ver", text.VersionId.ToString());
                cmd.Parameters.AddWithValue("$page", text.PageNumber);
                cmd.Parameters.AddWithValue("$content", text.Content);
                cmd.Parameters.AddWithValue("$conf", text.Confidence);
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "INSERT INTO DocumentTextFTS (DocumentId, VersionId, PageNumber, Content) VALUES ($doc, $ver, $page, $content)";
                fts.Parameters.AddWithValue("$doc", text.DocumentId.ToString());
                fts.Parameters.AddWithValue("$ver", text.VersionId.ToString());
                fts.Parameters.AddWithValue("$page", text.PageNumber);
                fts.Parameters.AddWithValue("$content", text.Content);
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentText.Add.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task<IEnumerable<DocumentText>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentText>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, VersionId, PageNumber, Content, Confidence FROM DocumentText WHERE DocumentId = $doc ORDER BY PageNumber";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentText
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    VersionId = Guid.Parse(reader.GetString(2)),
                    PageNumber = reader.GetInt32(3),
                    Content = reader.GetString(4),
                    Confidence = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5)
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
                cmd.CommandText = "DELETE FROM DocumentText WHERE DocumentId = $doc";
                cmd.Parameters.AddWithValue("$doc", documentId.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "DELETE FROM DocumentTextFTS WHERE DocumentId = $doc";
                fts.Parameters.AddWithValue("$doc", documentId.ToString());
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentText.Delete.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task UpdateAsync(DocumentText text, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE DocumentText SET Content = $content, Confidence = $conf WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", text.Id.ToString());
                cmd.Parameters.AddWithValue("$content", text.Content);
                cmd.Parameters.AddWithValue("$conf", text.Confidence);
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                var del = conn.CreateCommand();
                del.CommandText = "DELETE FROM DocumentTextFTS WHERE DocumentId = $doc AND VersionId = $ver AND PageNumber = $page";
                del.Parameters.AddWithValue("$doc", text.DocumentId.ToString());
                del.Parameters.AddWithValue("$ver", text.VersionId.ToString());
                del.Parameters.AddWithValue("$page", text.PageNumber);
                await del.ExecuteNonQueryAsync(cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "INSERT INTO DocumentTextFTS (DocumentId, VersionId, PageNumber, Content) VALUES ($doc, $ver, $page, $content)";
                fts.Parameters.AddWithValue("$doc", text.DocumentId.ToString());
                fts.Parameters.AddWithValue("$ver", text.VersionId.ToString());
                fts.Parameters.AddWithValue("$page", text.PageNumber);
                fts.Parameters.AddWithValue("$content", text.Content);
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentText.Update.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }
    }
}
