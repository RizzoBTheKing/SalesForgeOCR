using System;
using System.Collections.Generic;

namespace SalesForgeOCR.Core.Entities
{
    public class DocumentMetadata
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public DocumentMetadata()
        {
            Id = Guid.NewGuid();
        }

        public void Validate()
        {
            if (DocumentId == Guid.Empty) throw new ArgumentException("DocumentId is required");
            if (string.IsNullOrWhiteSpace(Key)) throw new ArgumentException("Key is required");
        }
    }
}
