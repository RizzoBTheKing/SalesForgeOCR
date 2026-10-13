namespace SalesForgeOCR.Core.Entities
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? CurrentVersionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
