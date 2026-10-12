using System;

namespace SalesForgeOCR.Core.Entities
{
    public class UserSession
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? Machine { get; set; }
        public string? IpAddress { get; set; }

        public UserSession()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = CreatedAt.AddHours(8);
        }

        public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;
    }
}
