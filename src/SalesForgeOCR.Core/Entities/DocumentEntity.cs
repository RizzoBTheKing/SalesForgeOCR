using System;

namespace SalesForgeOCR.Core.Entities
{
    public class DocumentEntity
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid VersionId { get; set; }
        public string Type { get; set; } = string.Empty; // e.g., Person, Email, VIN
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int PageNumber { get; set; }

        public DocumentEntity()
        {
            Id = Guid.NewGuid();
        }

        public void Validate()
        {
            if (DocumentId == Guid.Empty) throw new ArgumentException("DocumentId is required");
            if (string.IsNullOrWhiteSpace(Type)) throw new ArgumentException("Type is required");
            if (string.IsNullOrWhiteSpace(Value)) throw new ArgumentException("Value is required");
        }
    }
}
