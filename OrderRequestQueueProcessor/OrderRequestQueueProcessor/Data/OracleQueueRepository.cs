using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using OrderRequestQueueProcessor.Configuration;
using OrderRequestQueueProcessor.Models;
using OrderRequestQueueProcessor.Logging;

namespace OrderRequestQueueProcessor.Data
{
    public class OracleQueueRepository : IQueueRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<OracleQueueRepository> _logger;
        private readonly IHostEnvironment _hostEnv;

        public OracleQueueRepository(
            IOptions<AppSettings> options,
            ILogger<OracleQueueRepository> logger,
            IHostEnvironment hostEnv)
        {
            _connectionString = options.Value.OracleConnectionString;
            _logger = logger;
            _hostEnv = hostEnv;
        }

        public async Task<IReadOnlyList<OrderRequestQueueItem>> GetPendingRequestsAsync(
            int batchSize, CancellationToken cancellationToken)
        {
            const string component = "OracleQueueRepository.GetPendingRequestsAsync";
            var results = new List<OrderRequestQueueItem>();

            try
            {
                using var conn = new OracleConnection(_connectionString);

                // Try open connection; on failure, log and return empty
                try
                {
                    await conn.OpenAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogFailure(component, "Failed to open Oracle connection: {Error}", ex.Message);
                    return results;
                }

                // Step 1: Get eligible message IDs
                var idQuery = @"
                    SELECT iw.message_id
                    FROM inform_write.order_request_queue iw
                    WHERE UPPER(message_status) = 'PENDING'
                      AND ROWNUM <= :batchSize";

                var eligibleIds = new List<Guid>();
                try
                {
                    using var idCmd = new OracleCommand(idQuery, conn) { BindByName = true };
                    idCmd.Parameters.Add("batchSize", OracleDbType.Int32).Value = batchSize;

                    using var reader = await idCmd.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var idStr = reader.GetString(0)?.Trim();
                        if (Guid.TryParse(idStr, out var gid))
                        {
                            eligibleIds.Add(gid);
                        }
                        else
                        {
                            _logger.LogWarning("Malformed message_id returned from DB: {Value}", idStr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogFailure(component, "Error while fetching eligible IDs: {Error}", ex.Message);
                    return results;
                }

                if (eligibleIds.Count == 0)
                {
                    // No pending rows; return empty
                    return results;
                }

                // Step 2: Lock & fetch the full rows for those IDs
                var placeholders = string.Join(",", eligibleIds.Select((_, i) => $":id{i}"));
                var dataQuery = $@"
                    SELECT iw.message_id,
                           iw.message,
                           iw.message_status,
                           iw.message_retry_count,
                           iw.message_last_attempted_at,
                           iw.message_received_datetime,
                           iw.message_error_message
                    FROM inform_write.order_request_queue iw
                    WHERE message_id IN ({placeholders})
                    FOR UPDATE SKIP LOCKED";

                try
                {
                    using var dataCmd = new OracleCommand(dataQuery, conn) { BindByName = true };

                    for (int i = 0; i < eligibleIds.Count; i++)
                    {
                        dataCmd.Parameters.Add($"id{i}", OracleDbType.Varchar2, 36)
                              .Value = eligibleIds[i].ToString("D");
                    }

                    using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var idStr = reader.GetString(0);
                        if (!Guid.TryParse(idStr, out var gid))
                        {
                            _logger.LogWarning("Malformed message_id in data query: {Value}", idStr);
                            continue;
                        }

                        var item = new OrderRequestQueueItem
                        {
                            Id = gid,
                            Payload = reader.GetString(1),
                            Status = reader.GetString(2),
                            RetryCount = reader.GetInt32(3),
                            LastAttemptedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                            CreatedAt = reader.GetDateTime(5),
                            ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6)
                        };

                        results.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogFailure(component, "Error while locking/fetching rows: {Error}", ex.Message);
                    return results;
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogFailure(component, "Top-level error in GetPendingRequestsAsync: {Error}", ex.Message);
                return Array.Empty<OrderRequestQueueItem>();
            }
        }

        public async Task MarkAsCompletedAsync(Guid id, CancellationToken cancellationToken)
        {
            const string component = "OracleQueueRepository.MarkAsCompletedAsync";

            const string sql = @"
                UPDATE inform_write.order_request_queue
                   SET message_status = 'Completed',
                       message_last_attempted_at = SYSTIMESTAMP,
                       message_error_message = NULL
                 WHERE message_id = :id";

            try
            {
                await ExecuteNonQueryAsync(sql, id, null, cancellationToken);
                _logger.LogInfo(component, "Marked {Id} as Completed.", id);
            }
            catch (Exception ex)
            {
                _logger.LogFailure(component, "Error marking {Id} as Completed: {Error}", id, ex.Message);
            }
        }

        public async Task IncrementRetryAsync(Guid id, string errorMessage, CancellationToken cancellationToken)
        {
            const string component = "OracleQueueRepository.IncrementRetryAsync";

            const string sql = @"
                UPDATE inform_write.order_request_queue
                   SET message_retry_count = message_retry_count + 1,
                       message_status = CASE WHEN message_retry_count + 1 >= :maxRetry
                                             THEN 'DeadLetter'
                                             ELSE 'Failed' END,
                       message_last_attempted_at = SYSTIMESTAMP,
                       message_error_message = :error
                 WHERE message_id = :id";

            try
            {
                using var conn = new OracleConnection(_connectionString);
                using var cmd = new OracleCommand(sql, conn) { BindByName = true };

                cmd.Parameters.Add("maxRetry", OracleDbType.Int32).Value = 5; // consider moving to config
                cmd.Parameters.Add("error", OracleDbType.Varchar2).Value = errorMessage ?? string.Empty;
                cmd.Parameters.Add("id", OracleDbType.Varchar2, 36).Value = id.ToString("D");

                await conn.OpenAsync(cancellationToken);
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogWarning("Incremented retry for {Id}. Error: {Error}", id, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogFailure(component, "Error incrementing retry for {Id}: {Error}", id, ex.Message);
            }
        }

        public async Task MarkAsDeadLetterAsync(Guid id, string reason, CancellationToken cancellationToken)
        {
            const string component = "OracleQueueRepository.MarkAsDeadLetterAsync";

            const string sql = @"
                UPDATE inform_write.order_request_queue
                   SET message_status = 'DeadLetter',
                       message_last_attempted_at = SYSTIMESTAMP,
                       message_error_message = :reason
                 WHERE message_id = :id";

            try
            {
                await ExecuteNonQueryAsync(sql, id, reason, cancellationToken);
                _logger.LogWarning("Marked {Id} as DeadLetter. Reason: {Reason}", id, reason);
            }
            catch (Exception ex)
            {
                _logger.LogFailure(component, "Error marking {Id} as DeadLetter: {Error}", id, ex.Message);
            }
        }

        private async Task ExecuteNonQueryAsync(string sql, Guid id, string? reason, CancellationToken cancellationToken)
        {
            const string component = "OracleQueueRepository.ExecuteNonQueryAsync";

            try
            {
                using var conn = new OracleConnection(_connectionString);
                using var cmd = new OracleCommand(sql, conn) { BindByName = true };

                cmd.Parameters.Add("id", OracleDbType.Varchar2, 36).Value = id.ToString("D");
                if (sql.Contains(":reason"))
                {
                    cmd.Parameters.Add("reason", OracleDbType.Varchar2).Value = reason ?? string.Empty;
                }

                await conn.OpenAsync(cancellationToken);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogFailure(component, "Oracle nonquery failed for {Id}: {Error}", id, ex.Message);
            }
        }
    }
}
