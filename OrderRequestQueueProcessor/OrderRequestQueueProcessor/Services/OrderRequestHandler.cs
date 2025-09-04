using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderRequestQueueProcessor.Configuration;
using OrderRequestQueueProcessor.Models;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace OrderRequestQueueProcessor.Services
{
    public class OrderRequestHandler : IOrderRequestHandler
    {
        private readonly ILogger<OrderRequestHandler> _logger;
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly IOrderRequestService _orderRequestService;

        public OrderRequestHandler(IOptions<AppSettings> options, ILogger<OrderRequestHandler> logger, IOrderRequestService orderRequestService)
        {
            _settings = options.Value;
            _logger = logger;
            _httpClient = new HttpClient(); // You could inject IHttpClientFactory if preferred
            _orderRequestService = orderRequestService;
        }

        public async Task<ProcessResult> ProcessAsync(OrderRequestQueueItem queueItem, CancellationToken cancellationToken)
        {
            try
            {
                //OrderRequestPayloadDto? dto;
                try
                {
                    var payload = JsonConvert.DeserializeObject<OrderRequestPayloadDto>(queueItem.Payload);
                    //dto = JsonSerializer.Deserialize<OrderRequestPayloadDto>(queueItem.Payload);
                    if (payload == null)
                    {
                        return ProcessResult.Failure("Deserialized object was null.");
                    }
                    // Pass it to your service to save it
                    var newId = await _orderRequestService.CreateOrderRequestAsync(payload.Order_Request, cancellationToken);
                    _logger.LogInformation("OrderRequest ID {Id} created from QueueItem {QueueId}", newId, queueItem.Id);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Failed to deserialize OrderRequestQueueItem ID {Id}", queueItem.Id);
                    return ProcessResult.Failure("Invalid order request payload format.");
                }

                

                return ProcessResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing OrderRequestQueueItem ID {Id}", queueItem.Id);
                return ProcessResult.Failure(ex.Message);
            }

        }


    }
}
