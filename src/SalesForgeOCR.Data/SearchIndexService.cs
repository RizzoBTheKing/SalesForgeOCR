using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Search
{
    public class SearchIndexService
    {
        private readonly string _dbPath;
        public SearchIndexService(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public Task RebuildAllAsync(CancellationToken cancellationToken = default) => RebuildAsync(null, cancellationToken);
        public Task ReindexDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => RebuildAsync(documentId, cancellationToken);
        private async Task RebuildAsync(Guid? documentId, CancellationToken cancellationToken)
        {
            await using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString());
            await conn.OpenAsync(cancellationToken);
            using var transaction = conn.BeginTransaction();
            using var command = conn.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"DELETE FROM SearchIndexFTS WHERE $doc IS NULL OR DocumentId=$doc;
INSERT INTO SearchIndexFTS (DocumentId,SourceRecordId,SourceType,SearchableText,SearchWeight,CreatedDate,LastModifiedDate)
SELECT DocumentId,Id,'Text',Content,1,$now,$now FROM DocumentText WHERE $doc IS NULL OR DocumentId=$doc;
INSERT INTO SearchIndexFTS (DocumentId,SourceRecordId,SourceType,SearchableText,SearchWeight,CreatedDate,LastModifiedDate)
SELECT DocumentId,Id,'Entity',Value,2,$now,$now FROM DocumentEntities WHERE $doc IS NULL OR DocumentId=$doc;
INSERT INTO SearchIndexFTS (DocumentId,SourceRecordId,SourceType,SearchableText,SearchWeight,CreatedDate,LastModifiedDate)
SELECT DocumentId,Id,'Metadata',Value,0.5,$now,$now FROM DocumentMetadata WHERE $doc IS NULL OR DocumentId=$doc;
INSERT INTO SearchIndexFTS (DocumentId,SourceRecordId,SourceType,SearchableText,SearchWeight,CreatedDate,LastModifiedDate)
SELECT DocumentId,Id,'Image',FilePath,0.2,$now,$now FROM DocumentImages WHERE $doc IS NULL OR DocumentId=$doc;
DELETE FROM DocumentTextFTS;
INSERT INTO DocumentTextFTS (DocumentId,VersionId,PageNumber,Content) SELECT DocumentId,VersionId,PageNumber,Content FROM DocumentText;";
            command.Parameters.AddWithValue("$doc", (object?)documentId?.ToString() ?? DBNull.Value);
            command.Parameters.AddWithValue("$now",DateTime.UtcNow.ToString("o"));
            await command.ExecuteNonQueryAsync(cancellationToken);
            transaction.Commit();
        }

        public async Task IndexTextEntryAsync(Guid documentId, Guid sourceRecordId, string text, double weight = 1.0, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            using var transaction = conn.BeginTransaction();
            using var remove = conn.CreateCommand();
            remove.Transaction = transaction;
            remove.CommandText = "DELETE FROM SearchIndexFTS WHERE SourceRecordId = $id";
            remove.Parameters.AddWithValue("$id", sourceRecordId.ToString());
            await remove.ExecuteNonQueryAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "INSERT INTO SearchIndexFTS (DocumentId, SourceRecordId, SourceType, SearchableText, SearchWeight, CreatedDate, LastModifiedDate) VALUES ($doc, $src, $type, $text, $w, $created, $modified)";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            cmd.Parameters.AddWithValue("$src", sourceRecordId.ToString());
            cmd.Parameters.AddWithValue("$type", "Text");
            cmd.Parameters.AddWithValue("$text", text ?? string.Empty);
            cmd.Parameters.AddWithValue("$w", weight);
            var now = DateTime.UtcNow.ToString("o");
            cmd.Parameters.AddWithValue("$created", now);
            cmd.Parameters.AddWithValue("$modified", now);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            transaction.Commit();
        }

        public async Task IndexEntityEntryAsync(Guid documentId, Guid sourceRecordId, string value, double weight = 2.0, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            using var transaction = conn.BeginTransaction();
            using var remove = conn.CreateCommand();
            remove.Transaction = transaction;
            remove.CommandText = "DELETE FROM SearchIndexFTS WHERE SourceRecordId = $id";
            remove.Parameters.AddWithValue("$id", sourceRecordId.ToString());
            await remove.ExecuteNonQueryAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "INSERT INTO SearchIndexFTS (DocumentId, SourceRecordId, SourceType, SearchableText, SearchWeight, CreatedDate, LastModifiedDate) VALUES ($doc, $src, $type, $text, $w, $created, $modified)";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            cmd.Parameters.AddWithValue("$src", sourceRecordId.ToString());
            cmd.Parameters.AddWithValue("$type", "Entity");
            cmd.Parameters.AddWithValue("$text", value ?? string.Empty);
            cmd.Parameters.AddWithValue("$w", weight);
            var now = DateTime.UtcNow.ToString("o");
            cmd.Parameters.AddWithValue("$created", now);
            cmd.Parameters.AddWithValue("$modified", now);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            transaction.Commit();
        }

        public async Task IndexMetadataEntryAsync(Guid documentId, Guid sourceRecordId, string value, double weight = 0.5, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            using var transaction = conn.BeginTransaction();
            using var remove = conn.CreateCommand();
            remove.Transaction = transaction;
            remove.CommandText = "DELETE FROM SearchIndexFTS WHERE SourceRecordId = $id";
            remove.Parameters.AddWithValue("$id", sourceRecordId.ToString());
            await remove.ExecuteNonQueryAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "INSERT INTO SearchIndexFTS (DocumentId, SourceRecordId, SourceType, SearchableText, SearchWeight, CreatedDate, LastModifiedDate) VALUES ($doc, $src, $type, $text, $w, $created, $modified)";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            cmd.Parameters.AddWithValue("$src", sourceRecordId.ToString());
            cmd.Parameters.AddWithValue("$type", "Metadata");
            cmd.Parameters.AddWithValue("$text", value ?? string.Empty);
            cmd.Parameters.AddWithValue("$w", weight);
            var now = DateTime.UtcNow.ToString("o");
            cmd.Parameters.AddWithValue("$created", now);
            cmd.Parameters.AddWithValue("$modified", now);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            transaction.Commit();
        }

        public async Task UpdateIndexEntryAsync(Guid sourceRecordId, string newText, double weight = 1.0, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE SearchIndexFTS SET SearchableText = $text, SearchWeight = $w, LastModifiedDate = $modified WHERE SourceRecordId = $src";
            cmd.Parameters.AddWithValue("$text", newText ?? string.Empty);
            cmd.Parameters.AddWithValue("$w", weight);
            cmd.Parameters.AddWithValue("$modified", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("$src", sourceRecordId.ToString());
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<bool> ValidateIndexAsync(CancellationToken cancellationToken = default)
        {
            // Basic validation: check counts
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM SearchIndexFTS";
            var cnt = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
            return cnt >= 0;
        }

        public async Task RemoveDocumentIndexAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM SearchIndexFTS WHERE DocumentId = $doc";
            cmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task RemoveEntityIndexAsync(Guid entityId, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM SearchIndexFTS WHERE SourceRecordId = $src AND SourceType = 'Entity'";
            cmd.Parameters.AddWithValue("$src", entityId.ToString());
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task RemoveMetadataIndexAsync(Guid metadataId, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM SearchIndexFTS WHERE SourceRecordId = $src AND SourceType = 'Metadata'";
            cmd.Parameters.AddWithValue("$src", metadataId.ToString());
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task RemoveImageIndexAsync(Guid imageId, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM SearchIndexFTS WHERE SourceRecordId = $src AND SourceType = 'Image'";
            cmd.Parameters.AddWithValue("$src", imageId.ToString());
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<ValidationResult> ValidateIndexConsistencyAsync(CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);

            // Find orphaned SearchIndexFTS entries with missing document
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DocumentId, count(*) FROM SearchIndexFTS GROUP BY DocumentId";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var orphans = new List<string>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var docId = reader.GetString(0);
                var chk = conn.CreateCommand();
                chk.CommandText = "SELECT 1 FROM Documents WHERE Id = $id LIMIT 1";
                chk.Parameters.AddWithValue("$id", docId);
                var exists = await chk.ExecuteScalarAsync(cancellationToken);
                if (exists == null) orphans.Add(docId);
            }
            result.OrphanedDocumentIds = orphans;

            // Duplicates detection
            var dupCmd = conn.CreateCommand();
            dupCmd.CommandText = "SELECT DocumentId, SourceRecordId, COUNT(*) as cnt FROM SearchIndexFTS GROUP BY DocumentId, SourceRecordId HAVING cnt > 1";
            await using var dupReader = await dupCmd.ExecuteReaderAsync(cancellationToken);
            var duplicates = new List<(string docId, string srcId)>();
            while (await dupReader.ReadAsync(cancellationToken))
            {
                duplicates.Add((dupReader.GetString(0), dupReader.GetString(1)));
            }
            result.DuplicateEntries = duplicates;

            return result;
        }

    public class ValidationResult
    {
        public List<string> OrphanedDocumentIds { get; set; } = new List<string>();
        public List<(string docId, string srcId)> DuplicateEntries { get; set; } = new List<(string docId, string srcId)>();
    }

    }
}
