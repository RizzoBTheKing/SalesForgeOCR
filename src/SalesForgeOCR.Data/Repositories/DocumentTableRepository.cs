using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class DocumentTableRepository : SalesForgeOCR.Core.Interfaces.IDocumentTableRepository
    {
        private readonly string _dbPath;
        private readonly AuditRepository _audit;
        public DocumentTableRepository(string? dbPath = null, AuditRepository? audit = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
            _audit = audit ?? new AuditRepository(_dbPath);
        }

        public async Task AddAsync(DocumentTable table, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DocumentTables (Id, DocumentId, VersionId, PageNumber, Html) VALUES ($id, $doc, $ver, $page, $html)";
                cmd.Parameters.AddWithValue("$id", table.Id.ToString());
                cmd.Parameters.AddWithValue("$doc", table.DocumentId.ToString());
                cmd.Parameters.AddWithValue("$ver", table.VersionId.ToString());
                cmd.Parameters.AddWithValue("$page", table.PageNumber);
                cmd.Parameters.AddWithValue("$html", table.Html ?? string.Empty);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentTable.Add.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task<IEnumerable<DocumentTable>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentTable>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, VersionId, PageNumber, Html FROM DocumentTables WHERE DocumentId = $doc";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentTable
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    VersionId = Guid.Parse(reader.GetString(2)),
                    PageNumber = reader.GetInt32(3),
                    Html = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
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
                cmd.CommandText = "DELETE FROM DocumentTables WHERE DocumentId = $doc";
                cmd.Parameters.AddWithValue("$doc", documentId.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentTable.Delete.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }
    }
}
