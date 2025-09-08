using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderRequestQueueProcessor.Configuration;
using OrderRequestQueueProcessor.Models;
using OrderRequestQueueProcessor.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderRequestQueueProcessor.Logging;

namespace OrderRequestQueueProcessor.Services
{
    public class OrderRequestHandler(
        IOptions<AppSettings> options,
        ILogger<OrderRequestHandler> logger,
        IOrderRequestService orderRequestService) : IOrderRequestHandler
    {
        private readonly AppSettings _settings = options.Value;

        public async Task<ProcessResult> ProcessAsync(OrderRequestQueueItem queueItem, CancellationToken cancellationToken)
        {
            const string component = "OrderRequestHandler.ProcessAsync";

            try
            {
                // 0) Basic guard
                if (queueItem == null)
                {
                    logger.LogFailure(component, "Queue item was null.");
                    return ProcessResult.Failure("Queue item was null.");
                }

                if (string.IsNullOrWhiteSpace(queueItem.Payload))
                {
                    logger.LogFailure(component, "Queue item {Id} had an empty payload.", queueItem.Id);
                    return ProcessResult.Failure("Empty payload.");
                }

                // 1) Parse JSON
                JObject root;
                try
                {
                    root = JObject.Parse(queueItem.Payload);
                }
                catch (JsonReaderException ex)
                {
                    logger.LogFailure(component, "Payload for QueueItem {Id} is not valid JSON: {Error}", queueItem.Id, ex.Message);
                    return ProcessResult.Failure("Invalid JSON payload.");
                }

                // 2) Extract order_request object(s) — arrays only
                if (root.SelectToken("root.order_requests") is not JArray orderRequestsArray)
                {
                    logger.LogFailure(component, "Missing 'root.order_requests' in payload for QueueItem {Id}.", queueItem.Id);
                    return ProcessResult.Failure("Missing 'root.order_requests' in payload.");
                }

                logger.LogInfo(component, "Detected {Count} order_request items under 'root.order_requests'.", orderRequestsArray.Count);

                foreach (var item in orderRequestsArray)
                {
                    var orderReqObj =
                        (item as JObject)?.SelectToken("order_request") as JObject
                        ?? (item as JObject);

                    if (orderReqObj == null)
                    {
                        logger.LogFailure(component, "One element in 'root.order_requests' was not an object; skipping.");
                        continue;
                    }

                    // 3) Deserialize
                    OrderRequestPayloadDto payload;
                    try
                    {
                        payload = orderReqObj.ToObject<OrderRequestPayloadDto>() ?? new OrderRequestPayloadDto();
                    }
                    catch (Exception ex)
                    {
                        logger.LogFailure(component, "Failed to deserialize one order_request element: {Error}", ex.Message);
                        continue;
                    }

                    var dto = payload.Order_Request;
                    if (dto == null)
                    {
                        logger.LogFailure(component, "Deserialization produced null DTO for one order_request element.");
                        continue;
                    }

                    // 4) Persist via service
                    var newId = await orderRequestService.CreateOrderRequestAsync(dto, cancellationToken);
                    if (newId == null)
                    {
                        logger.LogFailure(component, "Service failed to create OrderRequest for one element in array; continuing with others.");
                        continue;
                    }

                    logger.LogInfo(component, "OrderRequest ID {Id} created from one element in array.", newId);
                }

                return ProcessResult.Success();
            }
            catch (JsonException jsonEx)
            {
                logger.LogFailure(component, "JSON error while processing QueueItem {Id}: {Error}", queueItem.Id, jsonEx.Message);
                return ProcessResult.Failure("Invalid order request payload format.");
            }
            catch (OperationCanceledException)
            {
                logger.LogFailure(component, "Operation cancelled while processing QueueItem {Id}.", queueItem.Id);
                return ProcessResult.Failure("Operation cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogFailure(component, "Unhandled error while processing QueueItem {Id}: {Error}", queueItem.Id, ex.Message);
                return ProcessResult.Failure(ex.Message);
            }
        }
    }
}
