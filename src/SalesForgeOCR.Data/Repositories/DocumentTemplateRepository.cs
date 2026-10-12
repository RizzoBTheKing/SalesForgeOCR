using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class DocumentTemplateRepository : SalesForgeOCR.Core.Interfaces.IDocumentTemplateRepository
    {
        private readonly string _dbPath;
        private readonly AuditRepository _audit;
        public DocumentTemplateRepository(string? dbPath = null, AuditRepository? audit = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
            _audit = audit ?? new AuditRepository(_dbPath);
        }

        public async Task AddAsync(DocumentTemplate template, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DocumentTemplates (Id, Name, Description, RulesJson, CreatedAt, CreatedBy) VALUES ($id, $name, $desc, $rules, $created, $by)";
                cmd.Parameters.AddWithValue("$id", template.Id.ToString());
                cmd.Parameters.AddWithValue("$name", template.Name);
                cmd.Parameters.AddWithValue("$desc", template.Description ?? string.Empty);
                cmd.Parameters.AddWithValue("$rules", template.RulesJson ?? string.Empty);
                cmd.Parameters.AddWithValue("$created", template.CreatedAt.ToString("o"));
                cmd.Parameters.AddWithValue("$by", template.CreatedBy ?? string.Empty);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentTemplate.Add.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task<IEnumerable<DocumentTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentTemplate>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Description, RulesJson, CreatedAt, CreatedBy FROM DocumentTemplates";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentTemplate
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    RulesJson = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    CreatedAt = reader.IsDBNull(4) ? DateTime.UtcNow : DateTime.Parse(reader.GetString(4)),
                    CreatedBy = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                });
            }
            return list;
        }

        public async Task<DocumentTemplate?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Description, RulesJson, CreatedAt, CreatedBy FROM DocumentTemplates WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new DocumentTemplate
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    RulesJson = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    CreatedAt = reader.IsDBNull(4) ? DateTime.UtcNow : DateTime.Parse(reader.GetString(4)),
                    CreatedBy = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                };
            }
            return null;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DocumentTemplates WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "DocumentTemplate.Delete.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }
    }
}
