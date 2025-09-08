using OrderRequestQueueProcessor.Models;

using OrderRequestQueueProcessor.Logging;
namespace OrderRequestQueueProcessor.Services
{
    public interface IOrderRequestHandler
    {
        Task<ProcessResult> ProcessAsync(OrderRequestQueueItem queueItem, CancellationToken cancellationToken);
    }
}
