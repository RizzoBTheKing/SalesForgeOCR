using System;

namespace SalesForgeOCR.Core.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Severity { get; set; } = "Information";

        public AuditLog()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
}
