namespace OrderRequestQueueProcessor.Models
{
    public class OrderRequestQueueItem
    {
        public Guid Id { get; set; }

        public string Payload { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed, DeadLetter

        public int RetryCount { get; set; }

        public DateTime? LastAttemptedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ErrorMessage { get; set; }
    }
}
