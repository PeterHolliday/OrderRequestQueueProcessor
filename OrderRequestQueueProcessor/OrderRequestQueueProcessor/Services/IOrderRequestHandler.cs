using OrderRequestQueueProcessor.Models;

namespace OrderRequestQueueProcessor.Services
{
    public interface IOrderRequestHandler
    {
        Task<ProcessResult> ProcessAsync(OrderRequestQueueItem queueItem, CancellationToken cancellationToken);
    }
}
