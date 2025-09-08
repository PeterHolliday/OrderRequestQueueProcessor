using OrderRequestQueueProcessor.Models;

using OrderRequestQueueProcessor.Logging;
namespace OrderRequestQueueProcessor.Data
{
    public interface IQueueRepository
    {
        /// <summary>
        /// Gets a batch of pending order request queue items, ordered by oldest first.
        /// </summary>
        Task<IReadOnlyList<OrderRequestQueueItem>> GetPendingRequestsAsync(
            int batchSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// Marks a request as completed.
        /// </summary>
        Task MarkAsCompletedAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Increments retry count and sets error message. Does not dead-letter directly.
        /// </summary>
        Task IncrementRetryAsync(Guid id, string errorMessage, CancellationToken cancellationToken);

        /// <summary>
        /// Marks a queue item as permanently failed (dead-letter).
        /// </summary>
        Task MarkAsDeadLetterAsync(Guid id, string reason, CancellationToken cancellationToken);
    }
}
