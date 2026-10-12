using System;

namespace SalesForgeOCR.Core.Entities
{
    public enum ProcessingStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }

    public class ProcessingJob
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid VersionId { get; set; }
        public ProcessingStatus Status { get; set; }
        public int Attempt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public ProcessingJob()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Status = ProcessingStatus.Pending;
            Attempt = 0;
        }

        public void MarkInProgress() { Status = ProcessingStatus.InProgress; StartedAt = DateTime.UtcNow; }
        public void MarkCompleted() { Status = ProcessingStatus.Completed; CompletedAt = DateTime.UtcNow; }
        public void MarkFailed(string error)
        {
            Status = ProcessingStatus.Failed;
            ErrorMessage = error;
            CompletedAt = DateTime.UtcNow;
        }
    }
}
