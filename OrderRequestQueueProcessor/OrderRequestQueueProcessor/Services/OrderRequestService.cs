using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderRequestQueueProcessor.Data;
using OrderRequestQueueProcessor.Models;
using OrderRequestQueueProcessor.Logging;

namespace OrderRequestQueueProcessor.Services
{
    public class OrderRequestService : IOrderRequestService
    {
        private readonly OrderRequestDbContext _dbContext;
        private readonly ILogger<OrderRequestService> _logger;

        public OrderRequestService(OrderRequestDbContext dbContext, ILogger<OrderRequestService> logger)
        {
            Console.WriteLine("[OrderRequestService.cs] ENTER OrderRequestService()");

            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<decimal?> CreateOrderRequestAsync(OrderRequestDto dto, CancellationToken cancellationToken)
        {
            const string component = "OrderRequestService";

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Map header + lines
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
                        // If your FK is set via the navigation, OrderRequestId can be omitted.
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

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInfo(component, "OrderRequest created with ID {Id}", entity.Id);
                return entity.Id;
            }
            catch (Exception ex)
            {
                try { await transaction.RollbackAsync(cancellationToken); } catch { }
                Console.WriteLine("[OrderRequestService.cs] CATCH -> " + (ex?.Message ?? "no message"));
                _logger.LogFailure(component, "Failed to create OrderRequest: {Error}", ex.Message);
                return null;
            }
        }
    }
}