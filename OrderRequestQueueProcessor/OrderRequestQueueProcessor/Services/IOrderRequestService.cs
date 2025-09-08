using OrderRequestQueueProcessor.Models;

using OrderRequestQueueProcessor.Logging;
namespace OrderRequestQueueProcessor.Services
{
    public interface IOrderRequestService
    {
        Task<decimal?> CreateOrderRequestAsync(OrderRequestDto dto, CancellationToken cancellationToken);
    }
}
