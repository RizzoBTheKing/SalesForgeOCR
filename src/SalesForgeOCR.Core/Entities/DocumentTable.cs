using System;
using System.Collections.Generic;

namespace SalesForgeOCR.Core.Entities
{
    public class DocumentTable
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid VersionId { get; set; }
        public int PageNumber { get; set; }
        public string Html { get; set; } = string.Empty; // store as HTML or JSON
        public List<List<string>> Cells { get; set; } = new List<List<string>>();

        public DocumentTable()
        {
            Id = Guid.NewGuid();
        }

        public void Validate()
        {
            if (DocumentId == Guid.Empty) throw new ArgumentException("DocumentId is required");
            if (VersionId == Guid.Empty) throw new ArgumentException("VersionId is required");
        }
    }
}
