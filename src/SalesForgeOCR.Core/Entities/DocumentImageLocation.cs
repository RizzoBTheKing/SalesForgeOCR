using System;

namespace SalesForgeOCR.Core.Entities
{
    public class DocumentImageLocation
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid ImageId { get; set; }
        public int PageNumber { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int ResolutionDpi { get; set; }

        public DocumentImageLocation()
        {
            Id = Guid.NewGuid();
        }

        public void Validate()
        {
            if (DocumentId == Guid.Empty) throw new ArgumentException("DocumentId is required");
            if (ImageId == Guid.Empty) throw new ArgumentException("ImageId is required");
            if (PageNumber < 1) throw new ArgumentException("PageNumber must be >= 1");
        }
    }
}
