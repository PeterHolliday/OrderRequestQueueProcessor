using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderRequestQueueProcessor.Configuration;
using OrderRequestQueueProcessor.Data;
using OrderRequestQueueProcessor.Services;

namespace OrderRequestQueueProcessor
{
    public class QueueProcessingService : BackgroundService
    {
        private readonly ILogger<QueueProcessingService> _logger;
        private readonly IQueueRepository _queueRepository;
        private readonly IOrderRequestHandler _orderRequestHandler;
        private readonly AppSettings _settings;

        public QueueProcessingService(
            ILogger<QueueProcessingService> logger,
            IQueueRepository queueRepository,
            IOrderRequestHandler orderRequestHandler,
            IOptions<AppSettings> options)
        {
            _logger = logger;
            _queueRepository = queueRepository;
            _orderRequestHandler = orderRequestHandler;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Request Queue Processing Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pendingRequests = await _queueRepository.GetPendingRequestsAsync(_settings.BatchSize, stoppingToken);

                    foreach (var request in pendingRequests)
                    {
                        var result = await _orderRequestHandler.ProcessAsync(request, stoppingToken);

                        if (result.IsSuccess)
                        {
                            await _queueRepository.MarkAsCompletedAsync(request.Id, stoppingToken);
                            _logger.LogInformation("Processed Order Request ID {Id} successfully.", request.Id);
                        }
                        else
                        {
                            await _queueRepository.IncrementRetryAsync(request.Id, result.ErrorMessage, stoppingToken);
                            _logger.LogWarning("Failed to process Order Request ID {Id}: {Error}", request.Id, result.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in processing loop.");
                }

                await Task.Delay(_settings.PollingIntervalSeconds * 1000, stoppingToken);
            }

            _logger.LogInformation("Order Request Queue Processing Service is stopping.");
        }
    }
}
