using OrderRequestQueueProcessor.Models;

namespace OrderRequestQueueProcessor.Services
{
    public interface IOrderRequestService
    {
        Task<long> CreateOrderRequestAsync(OrderRequestDto dto, CancellationToken cancellationToken);
    }
}
