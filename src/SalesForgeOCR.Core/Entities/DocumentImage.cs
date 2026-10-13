using System;

namespace SalesForgeOCR.Core.Entities
{
    public class DocumentImage
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid VersionId { get; set; }
        public int PageNumber { get; set; }
        public string DisplayLabel => $"{Role} - Page {PageNumber}: {System.IO.Path.GetFileName(FilePath)}";
        public string Role { get; set; } = "Body";
        public string FilePath { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public DateTime ExtractedAt { get; set; }
        public string Hash { get; set; } = string.Empty;

        public DocumentImage()
        {
            Id = Guid.NewGuid();
            ExtractedAt = DateTime.UtcNow;
        }

        public void Validate()
        {
            if (DocumentId == Guid.Empty) throw new ArgumentException("DocumentId is required");
            if (VersionId == Guid.Empty) throw new ArgumentException("VersionId is required");
            if (string.IsNullOrWhiteSpace(FilePath)) throw new ArgumentException("FilePath is required");
        }
    }
}
