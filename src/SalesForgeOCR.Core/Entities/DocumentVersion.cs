using System;

namespace SalesForgeOCR.Core.Entities
{
    public class DocumentVersion
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? StoragePath { get; set; }

        public DocumentVersion()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public void Validate()
        {
            if (DocumentId == Guid.Empty) throw new ArgumentException("DocumentId is required");
            if (VersionNumber < 1) throw new ArgumentException("VersionNumber must be >= 1");
            if (string.IsNullOrWhiteSpace(FileName)) throw new ArgumentException("FileName is required");
        }
    }
}
