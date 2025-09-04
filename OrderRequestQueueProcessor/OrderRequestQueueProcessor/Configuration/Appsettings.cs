namespace OrderRequestQueueProcessor.Configuration
{
    public class AppSettings
    {
        public string OracleConnectionString { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; } = 10;
        public int MaxRetryCount { get; set; } = 5;
        public int BatchSize { get; set; } = 10;
        public string DownstreamApiUrl { get; set; } = string.Empty;
        public string DeadLetterNotificationEmail { get; set; } = string.Empty;
    }
}
