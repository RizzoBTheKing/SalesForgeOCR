using System;

namespace SalesForgeOCR.Core.Entities
{
    public class ApplicationSetting
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ApplicationSetting()
        {
            Id = Guid.NewGuid();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
