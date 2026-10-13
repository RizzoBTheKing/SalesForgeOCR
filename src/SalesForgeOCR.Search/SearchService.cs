using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Search
{
    public class SearchService
    {
        private readonly string _dbPath;
        public SearchService(string? dbPath = null)
        {
            _dbPath = dbPath ?? "salesforgeocr.db";
        }

        public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Query)) return new SearchResponse();
            if (request.Pagination.Page < 1 || request.Pagination.PageSize < 1 || request.Pagination.PageSize > 1000) throw new ArgumentOutOfRangeException(nameof(request.Pagination));
            if (request.MatchType == SearchMatchType.Fuzzy) throw new NotSupportedException("Fuzzy matching is not supported by this search engine. Use prefix or phrase matching.");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = new SearchResponse();
            var results = new Dictionary<Guid, SearchResult>();

            var connStr = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync(cancellationToken);

            // Build MATCH query based on match type
            var q = BuildMatchQuery(request.Query, request.MatchType);

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT rowid, DocumentId, SourceRecordId, SourceType, SearchableText, bm25(SearchIndexFTS) AS score FROM SearchIndexFTS WHERE SearchIndexFTS MATCH $q AND ($source IS NULL OR SourceType = $source) ORDER BY score";
            cmd.Parameters.AddWithValue("$q", q);
            cmd.Parameters.AddWithValue("$source", (object?)request.SourceType?.ToString() ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var rows = new List<(long rowid, string docId, string srcId, string srcType, string text, double score)>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var rowid = reader.GetInt64(0);
                var docId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                var srcId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var srcType = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                var text = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                var score = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5);
                rows.Add((rowid, docId, srcId, srcType, text, score));
            }

            // Enrich rows by source type
            foreach (var r in rows)
            {
                if (!Guid.TryParse(r.docId, out var docGuid)) continue;
                if (!Guid.TryParse(r.srcId, out var srcGuid)) srcGuid = Guid.Empty;

                // ensure result container
                if (!results.TryGetValue(docGuid, out var sres))
                {
                    sres = new SearchResult { DocumentId = docGuid, DocumentName = string.Empty, Score = 0 };
                    results[docGuid] = sres;
                }

                var hit = new SearchHit
                {
                    DocumentId = docGuid,
                    SourceRecordId = srcGuid,
                    MatchValue = r.text,
                    Snippet = CreateSnippet(r.text, request.Query),
                    Score = -r.score,
                    SourceType = ParseSourceType(r.srcType),
                    CreatedDate = null,
                    LastModifiedDate = null
                };

                // Enrich based on source type
                switch (r.srcType)
                {
                    case "Text":
                        await EnrichTextAsync(conn, srcGuid, hit, cancellationToken);
                        break;
                    case "Entity":
                        await EnrichEntityAsync(conn, srcGuid, hit, cancellationToken);
                        break;
                    case "Metadata":
                        await EnrichMetadataAsync(conn, srcGuid, hit, cancellationToken);
                        break;
                    case "Image":
                        await EnrichImageAsync(conn, srcGuid, hit, cancellationToken);
                        break;
                    default:
                        break;
                }

                sres.Hits.Add(hit);
                sres.Score += hit.Score;
            }

            // Populate DocumentName and VersionId for results
            foreach (var kv in results)
            {
                var docId = kv.Key;
                var res = kv.Value;
                var cmdDoc = conn.CreateCommand();
                cmdDoc.CommandText = "SELECT FileName, CurrentVersionId FROM Documents WHERE Id = $id LIMIT 1";
                cmdDoc.Parameters.AddWithValue("$id", docId.ToString());
                await using var r = await cmdDoc.ExecuteReaderAsync(cancellationToken);
                if (await r.ReadAsync(cancellationToken))
                {
                    res.DocumentName = r.IsDBNull(0) ? string.Empty : r.GetString(0);
                    foreach (var hit in res.Hits) hit.DocumentName = res.DocumentName;
                }
            }

            foreach (var group in request.FilterGroups)
            {
                foreach (var result in results.Values)
                    result.Hits = result.Hits.Where(hit => group.MustMatchAll
                        ? group.Filters.All(filter => MatchesFilter(filter, result, hit))
                        : group.Filters.Any(filter => MatchesFilter(filter, result, hit))).ToList();
            }
            var matching = results.Values.Where(result => result.Hits.Count > 0).ToList();
            foreach (var result in matching) result.Score = result.Hits.Sum(hit => hit.Score);
            IEnumerable<SearchResult> ordered = request.Sort?.Field.ToLowerInvariant() switch
            {
                null or "score" => matching.OrderByDescending(result => result.Score),
                "documentname" => matching.OrderBy(result => result.DocumentName, StringComparer.OrdinalIgnoreCase),
                "documentid" => matching.OrderBy(result => result.DocumentId),
                _ => throw new NotSupportedException("Unsupported search sort field.")
            };
            if (request.Sort != null && ((request.Sort.Field.Equals("score", StringComparison.OrdinalIgnoreCase) && !request.Sort.Descending) || (!request.Sort.Field.Equals("score", StringComparison.OrdinalIgnoreCase) && request.Sort.Descending))) ordered = ordered.Reverse();
            response.Statistics.TotalMatches = matching.Sum(result => result.Hits.Count);
            response.Results = ordered.Skip(checked((request.Pagination.Page - 1) * request.Pagination.PageSize)).Take(request.Pagination.PageSize).ToList();
            sw.Stop();

            response.Statistics.QueryTimeMs = sw.Elapsed.TotalMilliseconds;
            return response;
        }

        private static bool MatchesFilter(SearchFilter filter, SearchResult result, SearchHit hit)
        {
            var actual = filter.Field.ToLowerInvariant() switch
            {
                "documentid" => result.DocumentId.ToString(),
                "documentname" => result.DocumentName,
                "sourcetype" => hit.SourceType.ToString(),
                "entitytype" => hit.EntityType ?? "",
                "metadatakey" => hit.MetadataKey ?? "",
                "pagenumber" => hit.PageNumber.ToString(),
                "matchvalue" => hit.MatchValue,
                _ => throw new NotSupportedException($"Unsupported search filter field: {filter.Field}")
            };
            var matches = actual.Equals(filter.Value,StringComparison.OrdinalIgnoreCase);
            return filter.Negate ? !matches : matches;
        }

        private static string BuildMatchQuery(string query, Core.Search.SearchMatchType matchType)
        {
            if (string.IsNullOrWhiteSpace(query)) return "";
            var escaped = "\"" + query.Replace("\"", "\"\"") + "\"";
            return matchType == SearchMatchType.Prefix ? escaped + "*" : escaped;
        }

        private static string CreateSnippet(string text, string query)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return text.Length <= 200 ? text : text.Substring(0, 200) + "...";
            var start = Math.Max(0, idx - 40);
            var len = Math.Min(200, text.Length - start);
            return (start > 0 ? "..." : string.Empty) + text.Substring(start, len) + (start + len < text.Length ? "..." : string.Empty);
        }

        private static SearchSourceType ParseSourceType(string src)
        {
            return src switch
            {
                "Text" => SearchSourceType.Text,
                "Entity" => SearchSourceType.Entity,
                "Metadata" => SearchSourceType.Metadata,
                "Template" => SearchSourceType.Template,
                "Image" => SearchSourceType.Image,
                _ => SearchSourceType.Text
            };
        }

        private static async Task EnrichTextAsync(SqliteConnection conn, Guid srcGuid, SearchHit hit, CancellationToken cancellationToken)
        {
            if (srcGuid == Guid.Empty) return;
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT VersionId, PageNumber, Content FROM DocumentText WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", srcGuid.ToString());
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await r.ReadAsync(cancellationToken))
            {
                if (!r.IsDBNull(0) && Guid.TryParse(r.GetString(0), out var vid)) { /* reserved */ }
                hit.PageNumber = r.IsDBNull(1) ? 0 : r.GetInt32(1);
                hit.MatchValue = r.IsDBNull(2) ? hit.MatchValue : r.GetString(2);
            }
        }

        private static async Task EnrichEntityAsync(SqliteConnection conn, Guid srcGuid, SearchHit hit, CancellationToken cancellationToken)
        {
            if (srcGuid == Guid.Empty) return;
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Type, Value, Confidence, PageNumber, VersionId FROM DocumentEntities WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", srcGuid.ToString());
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await r.ReadAsync(cancellationToken))
            {
                hit.EntityType = r.IsDBNull(0) ? string.Empty : r.GetString(0);
                hit.MatchValue = r.IsDBNull(1) ? hit.MatchValue : r.GetString(1);
                hit.Score = hit.Score; // keep score
                hit.PageNumber = r.IsDBNull(3) ? 0 : r.GetInt32(3);
            }
        }

        private static async Task EnrichMetadataAsync(SqliteConnection conn, Guid srcGuid, SearchHit hit, CancellationToken cancellationToken)
        {
            if (srcGuid == Guid.Empty) return;
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Key, Value FROM DocumentMetadata WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", srcGuid.ToString());
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await r.ReadAsync(cancellationToken))
            {
                hit.MetadataKey = r.IsDBNull(0) ? string.Empty : r.GetString(0);
                hit.MatchValue = r.IsDBNull(1) ? hit.MatchValue : r.GetString(1);
            }
        }

        private static async Task EnrichImageAsync(SqliteConnection conn, Guid srcGuid, SearchHit hit, CancellationToken cancellationToken)
        {
            if (srcGuid == Guid.Empty) return;
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PageNumber, FilePath, ThumbnailPath, Hash FROM DocumentImages WHERE Id = $id LIMIT 1";
            cmd.Parameters.AddWithValue("$id", srcGuid.ToString());
            await using var r = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await r.ReadAsync(cancellationToken))
            {
                hit.PageNumber = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                hit.MatchValue = r.IsDBNull(1) ? hit.MatchValue : r.GetString(1);
            }
        }
    }
}
