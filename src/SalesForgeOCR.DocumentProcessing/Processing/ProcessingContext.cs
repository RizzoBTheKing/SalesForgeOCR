using SalesForgeOCR.Core.Entities;
using System;
using System.Collections.Generic;

namespace SalesForgeOCR.DocumentProcessing.Processing
{
    public class ProcessingContext
    {
        public string? RenderedPdfPath { get; set; }
        public Document Document { get; }
        public DocumentVersion Version { get; }
        public List<DocumentText> ExtractedText { get; } = new List<DocumentText>();
        public List<DocumentImage> Images { get; } = new List<DocumentImage>();
        public List<DocumentEntity> Entities { get; } = new List<DocumentEntity>();
        public List<DocumentTable> Tables { get; } = new List<DocumentTable>();
        public List<DocumentMetadata> Metadata { get; } = new List<DocumentMetadata>();

        public ProcessingContext(Document doc, DocumentVersion version)
        {
            Document = doc ?? throw new ArgumentNullException(nameof(doc));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }
    }
}
