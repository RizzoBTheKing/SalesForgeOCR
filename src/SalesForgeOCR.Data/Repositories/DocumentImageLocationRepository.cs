using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Data.Repositories
{
    public class DocumentImageLocationRepository : SalesForgeOCR.Core.Interfaces.IDocumentImageLocationRepository
    {
        private readonly string _dbPath;
        private readonly AuditRepository _audit;
        public DocumentImageLocationRepository(string? dbPath = null, AuditRepository? audit = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
            _audit = audit ?? new AuditRepository(_dbPath);
        }

        public async Task AddAsync(DocumentImageLocation loc, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DocumentImageLocations (Id, DocumentId, ImageId, PageNumber, X, Y, Width, Height, ResolutionDpi) VALUES ($id, $doc, $img, $page, $x, $y, $w, $h, $dpi)";
                cmd.Parameters.AddWithValue("$id", loc.Id.ToString());
                cmd.Parameters.AddWithValue("$doc", loc.DocumentId.ToString());
                cmd.Parameters.AddWithValue("$img", loc.ImageId.ToString());
                cmd.Parameters.AddWithValue("$page", loc.PageNumber);
                cmd.Parameters.AddWithValue("$x", loc.X);
                cmd.Parameters.AddWithValue("$y", loc.Y);
                cmd.Parameters.AddWithValue("$w", loc.Width);
                cmd.Parameters.AddWithValue("$h", loc.Height);
                cmd.Parameters.AddWithValue("$dpi", loc.ResolutionDpi);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "ImageLocation.Add.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }

        public async Task<IEnumerable<DocumentImageLocation>> GetByImageAsync(Guid imageId, CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentImageLocation>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, ImageId, PageNumber, X, Y, Width, Height, ResolutionDpi FROM DocumentImageLocations WHERE ImageId = $img";
            cmd.Parameters.AddWithValue("$img", imageId.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentImageLocation
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    ImageId = Guid.Parse(reader.GetString(2)),
                    PageNumber = reader.GetInt32(3),
                    X = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                    Y = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                    Width = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                    Height = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                    ResolutionDpi = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                });
            }
            return list;
        }

        public async Task<IEnumerable<DocumentImageLocation>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var list = new List<DocumentImageLocation>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, DocumentId, ImageId, PageNumber, X, Y, Width, Height, ResolutionDpi FROM DocumentImageLocations WHERE DocumentId = $doc";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new DocumentImageLocation
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DocumentId = Guid.Parse(reader.GetString(1)),
                    ImageId = Guid.Parse(reader.GetString(2)),
                    PageNumber = reader.GetInt32(3),
                    X = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                    Y = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                    Width = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                    Height = reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
                    ResolutionDpi = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                });
            }
            return list;
        }

        public async Task DeleteByImageAsync(Guid imageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
                await using var conn = new SqliteConnection(connStr);
                await conn.OpenAsync(cancellationToken);
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM DocumentImageLocations WHERE ImageId = $img";
                cmd.Parameters.AddWithValue("$img", imageId.ToString());
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _audit.InsertAsync(new AuditLog { EventType = "ImageLocation.Delete.Failure", Actor = "system", Details = ex.Message });
                throw;
            }
        }
    }
}
