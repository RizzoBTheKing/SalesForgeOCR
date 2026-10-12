using System;

namespace SalesForgeOCR.Core.Entities
{
    public class DocumentText
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid VersionId { get; set; }
        public int PageNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public double Confidence { get; set; }

        public DocumentText()
        {
            Id = Guid.NewGuid();
        }

        public void Validate()
        {
            if (DocumentId == Guid.Empty) throw new ArgumentException("DocumentId is required");
            if (VersionId == Guid.Empty) throw new ArgumentException("VersionId is required");
            if (PageNumber < 1) throw new ArgumentException("PageNumber must be >= 1");
        }
    }
}
