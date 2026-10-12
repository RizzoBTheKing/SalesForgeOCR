using System;
using System.Collections.Generic;

namespace SalesForgeOCR.Core.Search
{
    public enum SearchMatchType
    {
        Exact,
        Phrase,
        Prefix,
        Fuzzy
    }

    public enum SearchSourceType
    {
        Text,
        Entity,
        Metadata,
        Template,
        Image
    }

    public class SearchFilter
    {
        public string Field { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Negate { get; set; }
    }

    public class SearchFilterGroup
    {
        public bool MustMatchAll { get; set; } = true;
        public List<SearchFilter> Filters { get; set; } = new List<SearchFilter>();
    }

    public class SearchSortOption
    {
        public string Field { get; set; } = "score";
        public bool Descending { get; set; } = true;
    }

    public class SearchPagination
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public SearchMatchType MatchType { get; set; } = SearchMatchType.Prefix;
        public SearchSourceType? SourceType { get; set; }
        public List<SearchFilterGroup> FilterGroups { get; set; } = new List<SearchFilterGroup>();
        public SearchSortOption? Sort { get; set; }
        public SearchPagination Pagination { get; set; } = new SearchPagination();
    }

    public class SearchHit
    {
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string MatchValue { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public double Score { get; set; }
        public SearchSourceType SourceType { get; set; }
        public Guid SourceRecordId { get; set; }
        public int PageNumber { get; set; }
        public string? EntityType { get; set; }
        public string? MetadataKey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    public class SearchResult
    {
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public double Score { get; set; }
        public List<SearchHit> Hits { get; set; } = new List<SearchHit>();
    }

    public class SearchStatistics
    {
        public long TotalMatches { get; set; }
        public double QueryTimeMs { get; set; }
    }

    public class SearchResponse
    {
        public List<SearchResult> Results { get; set; } = new List<SearchResult>();
        public SearchStatistics Statistics { get; set; } = new SearchStatistics();
    }
}
