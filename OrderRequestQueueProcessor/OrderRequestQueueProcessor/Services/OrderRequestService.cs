using Microsoft.Extensions.Logging;
using OrderRequestQueueProcessor.Data;
using OrderRequestQueueProcessor.Models;


namespace OrderRequestQueueProcessor.Services
{
    public class OrderRequestService : IOrderRequestService
    {
        private readonly OrderRequestDbContext _dbContext;
        private readonly ILogger<OrderRequestService> _logger;

        public OrderRequestService(OrderRequestDbContext dbContext, ILogger<OrderRequestService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<long> CreateOrderRequestAsync(OrderRequestDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var entity = new OrderRequestDto
                {
                    AccountId = dto.AccountId,
                    PortalRequestId = dto.PortalRequestId,
                    DeliveryDate = dto.DeliveryDate,
                    TownId = dto.TownId,
                    DateEntered = dto.DateEntered,
                    ContactId = dto.ContactId,
                    CustomerOrderNo = dto.CustomerOrderNo,
                    DepotId = dto.DepotId,
                    Time = dto.Time,
                    Status = dto.Status,
                    SiteContactId = dto.SiteContactId,

                    OrderRequestLines = dto.OrderRequestLines.Select(line => new OrderRequestLineDto
                    {
                        OrderRequestId = line.OrderRequestId,
                        LineNo = line.LineNo,
                        ProductId = line.ProductId,
                        RateType = line.RateType,
                        Quantity = line.Quantity,
                        Identifier = line.Identifier,
                        Position = line.Position
                    }).ToList()
                };

                _dbContext.OrderRequests.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("OrderRequest created with ID {Id}", entity.Id);

                return entity.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create OrderRequest");
                throw;
            }
        }
    }
}
