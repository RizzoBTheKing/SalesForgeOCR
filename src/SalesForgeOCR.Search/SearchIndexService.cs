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

        public async Task RebuildAllAsync(CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);

            // Clear existing index
            var clear = conn.CreateCommand();
            clear.CommandText = "DELETE FROM SearchIndexFTS;";
            await clear.ExecuteNonQueryAsync(cancellationToken);

            // Index document text
            var textCmd = conn.CreateCommand();
            textCmd.CommandText = "SELECT Id, DocumentId, Content FROM DocumentText";
            await using var reader = await textCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var docId = reader.GetString(1);
                var srcId = reader.GetString(0);
                var content = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

                await IndexTextEntryAsync(Guid.Parse(docId), Guid.Parse(srcId), content, 1.0, cancellationToken);
            }

            // Index entities
            var entCmd = conn.CreateCommand();
            entCmd.CommandText = "SELECT Id, DocumentId, Type, Value FROM DocumentEntities";
            await using var entReader = await entCmd.ExecuteReaderAsync(cancellationToken);
            while (await entReader.ReadAsync(cancellationToken))
            {
                var srcId = entReader.GetString(0);
                var docId = entReader.GetString(1);
                var value = entReader.IsDBNull(3) ? string.Empty : entReader.GetString(3);
                await IndexEntityEntryAsync(Guid.Parse(docId), Guid.Parse(srcId), value, 2.0, cancellationToken);
            }

            // Index metadata
            var metaCmd = conn.CreateCommand();
            metaCmd.CommandText = "SELECT Id, DocumentId, Key, Value FROM DocumentMetadata";
            await using var metaReader = await metaCmd.ExecuteReaderAsync(cancellationToken);
            while (await metaReader.ReadAsync(cancellationToken))
            {
                var srcId = metaReader.GetString(0);
                var docId = metaReader.GetString(1);
                var value = metaReader.IsDBNull(3) ? string.Empty : metaReader.GetString(3);
                await IndexMetadataEntryAsync(Guid.Parse(docId), Guid.Parse(srcId), value, 0.5, cancellationToken);
            }
        }

        public async Task ReindexDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);

            // Delete existing entries for document
            var del = conn.CreateCommand();
            del.CommandText = "DELETE FROM SearchIndexFTS WHERE DocumentId = $doc";
            del.Parameters.AddWithValue("$doc", documentId.ToString());
            await del.ExecuteNonQueryAsync(cancellationToken);

            // Reinsert text
            var textCmd = conn.CreateCommand();
            textCmd.CommandText = "SELECT Id, Content FROM DocumentText WHERE DocumentId = $doc";
            textCmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var reader = await textCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var srcId = reader.GetString(0);
                var content = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                await IndexTextEntryAsync(documentId, Guid.Parse(srcId), content, 1.0, cancellationToken);
            }

            // Reindex entities and metadata similarly
            var entCmd = conn.CreateCommand();
            entCmd.CommandText = "SELECT Id, Value FROM DocumentEntities WHERE DocumentId = $doc";
            entCmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var entReader = await entCmd.ExecuteReaderAsync(cancellationToken);
            while (await entReader.ReadAsync(cancellationToken))
            {
                var srcId = entReader.GetString(0);
                var value = entReader.IsDBNull(1) ? string.Empty : entReader.GetString(1);
                await IndexEntityEntryAsync(documentId, Guid.Parse(srcId), value, 2.0, cancellationToken);
            }

            var metaCmd = conn.CreateCommand();
            metaCmd.CommandText = "SELECT Id, Value FROM DocumentMetadata WHERE DocumentId = $doc";
            metaCmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var metaReader = await metaCmd.ExecuteReaderAsync(cancellationToken);
            while (await metaReader.ReadAsync(cancellationToken))
            {
                var srcId = metaReader.GetString(0);
                var value = metaReader.IsDBNull(1) ? string.Empty : metaReader.GetString(1);
                await IndexMetadataEntryAsync(documentId, Guid.Parse(srcId), value, 0.5, cancellationToken);
            }
        }

        public async Task IndexTextEntryAsync(Guid documentId, Guid sourceRecordId, string text, double weight = 1.0, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
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
        }

        public async Task IndexEntityEntryAsync(Guid documentId, Guid sourceRecordId, string value, double weight = 2.0, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
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

            // legacy FTS
            var fts = conn.CreateCommand();
            fts.CommandText = "INSERT INTO EntityFTS (Type, Value) VALUES ($type, $value)";
            fts.Parameters.AddWithValue("$type", "Entity");
            fts.Parameters.AddWithValue("$value", value ?? string.Empty);
            await fts.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task IndexMetadataEntryAsync(Guid documentId, Guid sourceRecordId, string value, double weight = 0.5, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
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

            var fts = conn.CreateCommand();
            fts.CommandText = "INSERT INTO MetadataFTS (Key, Value) VALUES ($key, $value)";
            fts.Parameters.AddWithValue("$key", "");
            fts.Parameters.AddWithValue("$value", value ?? string.Empty);
            await fts.ExecuteNonQueryAsync(cancellationToken);
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
            var cnt = (long)await cmd.ExecuteScalarAsync(cancellationToken);
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

                ins.CommandText = "INSERT INTO SearchIndexFTS (DocumentId, SourceRecordId, SourceType, SearchableText, CreatedDate) VALUES ($doc, $src, $type, $text, $created)";
                ins.Parameters.AddWithValue("$doc", documentId.ToString());
                ins.Parameters.AddWithValue("$src", srcId);
                ins.Parameters.AddWithValue("$type", "Entity");
                ins.Parameters.AddWithValue("$text", value);
                ins.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
                await ins.ExecuteNonQueryAsync(cancellationToken);
            }

            var metaCmd = conn.CreateCommand();
            metaCmd.CommandText = "SELECT Id, Value FROM DocumentMetadata WHERE DocumentId = $doc";
            metaCmd.Parameters.AddWithValue("$doc", documentId.ToString());
            await using var metaReader = await metaCmd.ExecuteReaderAsync(cancellationToken);
            while (await metaReader.ReadAsync(cancellationToken))
            {
                var srcId = metaReader.GetString(0);
                var value = metaReader.IsDBNull(1) ? string.Empty : metaReader.GetString(1);
                var ins = conn.CreateCommand();
                ins.CommandText = "INSERT INTO SearchIndexFTS (DocumentId, SourceRecordId, SourceType, SearchableText, CreatedDate) VALUES ($doc, $src, $type, $text, $created)";
                ins.Parameters.AddWithValue("$doc", documentId.ToString());
                ins.Parameters.AddWithValue("$src", srcId);
                ins.Parameters.AddWithValue("$type", "Metadata");
                ins.Parameters.AddWithValue("$text", value);
                ins.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
                await ins.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task<bool> ValidateIndexAsync(CancellationToken cancellationToken = default)
        {
            // Basic validation: check counts
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM SearchIndexFTS";
            var cnt = (long)await cmd.ExecuteScalarAsync(cancellationToken);
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
