using System;

namespace SalesForgeOCR.Core.Entities
{
    public class DocumentTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RulesJson { get; set; } = string.Empty; // store template rules/metadata as JSON
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public DocumentTemplate()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("Template Name is required");
            if (string.IsNullOrWhiteSpace(RulesJson)) throw new ArgumentException("RulesJson is required");
        }
    }
}
