using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderRequestQueueProcessor.Configuration;
using OrderRequestQueueProcessor.Data;
using OrderRequestQueueProcessor.Services;

using OrderRequestQueueProcessor.Logging;
namespace OrderRequestQueueProcessor
{
    public class QueueProcessingService : BackgroundService
    {
        private readonly ILogger<QueueProcessingService> _logger;
        private readonly IQueueRepository _queueRepository;
        private readonly AppSettings _settings;
        private readonly IServiceScopeFactory _scopeFactory;

        public QueueProcessingService(
            ILogger<QueueProcessingService> logger,
            IQueueRepository queueRepository,
            IOptions<AppSettings> options,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _queueRepository = queueRepository;
            _settings = options.Value;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pendingRequests = await _queueRepository.GetPendingRequestsAsync(_settings.BatchSize, stoppingToken);

                    if (pendingRequests.Count > 0)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<IOrderRequestHandler>();

                        foreach (var request in pendingRequests)
                        {
                            var result = await handler.ProcessAsync(request, stoppingToken);

                            if (result.IsSuccess)
                            {
                                await _queueRepository.MarkAsCompletedAsync(request.Id, stoppingToken);
                                _logger.LogInfo("QueueProcessingService", "Processed Order Request ID {Id} successfully.", request.Id);
                            }
                            else
                            {
                                await _queueRepository.IncrementRetryAsync(request.Id, result.ErrorMessage ?? string.Empty, stoppingToken);
                                _logger.LogWarning("Failed to process Order Request ID {Id}: {Error}", request.Id, result.ErrorMessage);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogFailure("QueueProcessingService", "Unhandled exception in processing loop.: {Error}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
            }

            // removed trivial start/stop log
        }
    }
}
