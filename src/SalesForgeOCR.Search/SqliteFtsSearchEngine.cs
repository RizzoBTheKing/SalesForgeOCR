using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Search
{
    public class SqliteFtsSearchEngine : ISearchEngine
    {
        private readonly string _dbPath;
        public SqliteFtsSearchEngine(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public async Task IndexDocumentTextAsync(Guid documentId, Guid versionId, IEnumerable<(int page, string content)> pages, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            foreach (var (page, content) in pages)
            {
                var textId = Guid.NewGuid();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO DocumentText (Id, DocumentId, VersionId, PageNumber, Content, Confidence) VALUES ($id, $doc, $ver, $page, $content, $conf)";
                cmd.Parameters.AddWithValue("$id", textId.ToString());
                cmd.Parameters.AddWithValue("$doc", documentId.ToString());
                cmd.Parameters.AddWithValue("$ver", versionId.ToString());
                cmd.Parameters.AddWithValue("$page", page);
                cmd.Parameters.AddWithValue("$content", content);
                cmd.Parameters.AddWithValue("$conf", 1.0);
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "INSERT INTO SearchIndexFTS (DocumentId, SourceRecordId, SourceType, SearchableText, CreatedDate) VALUES ($doc, $src, $type, $text, $created)";
                fts.Parameters.AddWithValue("$doc", documentId.ToString());
                fts.Parameters.AddWithValue("$src", textId.ToString());
                fts.Parameters.AddWithValue("$type", "Text");
                fts.Parameters.AddWithValue("$text", content);
                fts.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task IndexEntitiesAsync(Guid documentId, IEnumerable<(string type, string value)> entities, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            foreach (var (type, value) in entities)
            {
                var srcId = Guid.NewGuid();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO SearchIndexFTS (DocumentId, SourceRecordId, SourceType, SearchableText, CreatedDate) VALUES ($doc, $src, $type, $text, $created)";
                cmd.Parameters.AddWithValue("$doc", documentId.ToString());
                cmd.Parameters.AddWithValue("$src", srcId.ToString());
                cmd.Parameters.AddWithValue("$type", "Entity");
                cmd.Parameters.AddWithValue("$text", value);
                cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // legacy table
                var fts = conn.CreateCommand();
                fts.CommandText = "INSERT INTO EntityFTS (Type, Value) VALUES ($type, $value)";
                fts.Parameters.AddWithValue("$type", type);
                fts.Parameters.AddWithValue("$value", value);
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task IndexMetadataAsync(Guid documentId, IEnumerable<(string key, string value)> metadata, CancellationToken cancellationToken = default)
        {
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            foreach (var (key, value) in metadata)
            {
                var srcId = Guid.NewGuid();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO SearchIndexFTS (DocumentId, SourceRecordId, SourceType, SearchableText, CreatedDate) VALUES ($doc, $src, $type, $text, $created)";
                cmd.Parameters.AddWithValue("$doc", documentId.ToString());
                cmd.Parameters.AddWithValue("$src", srcId.ToString());
                cmd.Parameters.AddWithValue("$type", "Metadata");
                cmd.Parameters.AddWithValue("$text", value);
                cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                var fts = conn.CreateCommand();
                fts.CommandText = "INSERT INTO MetadataFTS (Key, Value) VALUES ($key, $value)";
                fts.Parameters.AddWithValue("$key", key);
                fts.Parameters.AddWithValue("$value", value);
                await fts.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<(Guid documentId, double score)>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
        {
            var results = new List<(Guid, double)>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DocumentId, bm25(SearchIndexFTS) AS score FROM SearchIndexFTS WHERE SearchableText MATCH $q ORDER BY score LIMIT $limit";
            cmd.Parameters.AddWithValue("$q", query);
            cmd.Parameters.AddWithValue("$limit", limit);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = Guid.Parse(reader.GetString(0));
                var score = reader.IsDBNull(1) ? 0.0 : reader.GetDouble(1);
                results.Add((id, score));
            }
            return results;
        }

        public async Task<IEnumerable<(Guid documentId, double score)>> SearchEntitiesAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
        {
            var results = new List<(Guid, double)>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DocumentId, bm25(SearchIndexFTS) AS score FROM SearchIndexFTS WHERE SearchableText MATCH $q AND SourceType = 'Entity' ORDER BY score LIMIT $limit";
            cmd.Parameters.AddWithValue("$q", query);
            cmd.Parameters.AddWithValue("$limit", limit);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = Guid.Parse(reader.GetString(0));
                var score = reader.IsDBNull(1) ? 0.0 : reader.GetDouble(1);
                results.Add((id, score));
            }
            return results;
        }

        public async Task<IEnumerable<(Guid documentId, double score)>> SearchMetadataAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
        {
            var results = new List<(Guid, double)>();
            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DocumentId, bm25(SearchIndexFTS) AS score FROM SearchIndexFTS WHERE SearchableText MATCH $q AND SourceType = 'Metadata' ORDER BY score LIMIT $limit";
            cmd.Parameters.AddWithValue("$q", query);
            cmd.Parameters.AddWithValue("$limit", limit);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = Guid.Parse(reader.GetString(0));
                var score = reader.IsDBNull(1) ? 0.0 : reader.GetDouble(1);
                results.Add((id, score));
            }
            return results;
        }
    }
}
