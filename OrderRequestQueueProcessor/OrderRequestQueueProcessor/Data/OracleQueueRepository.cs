using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using OrderRequestQueueProcessor.Configuration;
using OrderRequestQueueProcessor.Models;

namespace OrderRequestQueueProcessor.Data
{
    public class OracleQueueRepository : IQueueRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<OracleQueueRepository> _logger;

        public OracleQueueRepository(IOptions<AppSettings> options, ILogger<OracleQueueRepository> logger)
        {
            _connectionString = options.Value.OracleConnectionString;
            _logger = logger;
        }

        public async Task<IReadOnlyList<OrderRequestQueueItem>> GetPendingRequestsAsync(int batchSize, CancellationToken cancellationToken)
        {
            var results = new List<OrderRequestQueueItem>();

            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            // Step 1: Get eligible IDs
            var idQuery = @"
                SELECT message_id
                FROM order_request_queue
                WHERE message_status = 'Pending'
                AND ROWNUM <= :batchSize";

            var eligibleIds = new List<long>();

            using (var idCmd = new OracleCommand(idQuery, conn))
            {
                idCmd.BindByName = true;
                idCmd.Parameters.Add("batchSize", OracleDbType.Int32).Value = batchSize;

                using var reader = await idCmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    eligibleIds.Add(reader.GetInt64(0));
                }
            }

            if (eligibleIds.Count == 0)
                return results;

            // Step 2: Lock & fetch the full rows
            var placeholders = string.Join(",", eligibleIds.Select((_, i) => $":id{i}"));
            var dataQuery = $@"
                SELECT message_id, message, message_status, message_retry_count, message_last_attempted_at, message_received_datetime, message_error_message
                FROM order_request_queue
                WHERE message_id IN ({placeholders})
                FOR UPDATE SKIP LOCKED";

            using (var dataCmd = new OracleCommand(dataQuery, conn))
            {
                dataCmd.BindByName = true;

                for (int i = 0; i < eligibleIds.Count; i++)
                {
                    dataCmd.Parameters.Add($":id{i}", OracleDbType.Int64).Value = eligibleIds[i];
                }

                using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(new OrderRequestQueueItem
                    {
                        Id = reader.GetInt64(0),
                        Payload = reader.GetString(1),
                        Status = reader.GetString(2),
                        RetryCount = reader.GetInt32(3),
                        LastAttemptedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                        CreatedAt = reader.GetDateTime(5),
                        ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6)
                    });
                }
            }

            return results;
        }


        public async Task MarkAsCompletedAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = @"
                UPDATE order_request_queue
                SET message_status = 'Completed',
                    message_last_attempted_at = SYSTIMESTAMP,
                    message_error_message = NULL
                WHERE message_id = :id";

            await ExecuteNonQueryAsync(sql, id, null, cancellationToken);
        }

        public async Task IncrementRetryAsync(long id, string errorMessage, CancellationToken cancellationToken)
        {
            const string sql = @"
                UPDATE order_request_queue
                SET message_retry_count = message_retry_count + 1,
                    message_status = CASE WHEN message_retry_count + 1 >= :maxRetry THEN 'DeadLetter' ELSE 'Failed' END,
                    message_last_attempted_at = SYSTIMESTAMP,
                    message_error_message = :error
                WHERE message_id = :id";

            using var conn = new OracleConnection(_connectionString);
            using var cmd = new OracleCommand(sql, conn);
            cmd.BindByName = true;

            cmd.Parameters.Add("maxRetry", OracleDbType.Int32).Value = 5; // Can be moved to config if needed
            cmd.Parameters.Add("error", OracleDbType.Varchar2).Value = errorMessage;
            cmd.Parameters.Add("id", OracleDbType.Int64).Value = id;

            await conn.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task MarkAsDeadLetterAsync(long id, string reason, CancellationToken cancellationToken)
        {
            const string sql = @"
                UPDATE order_request_queue
                SET message_status = 'DeadLetter',
                    message_last_attempted_at = SYSTIMESTAMP,
                    message_error_message = :reason
                WHERE id = :id";

            await ExecuteNonQueryAsync(sql, id, reason, cancellationToken);
        }

        private async Task ExecuteNonQueryAsync(string sql, long id, string? reason, CancellationToken cancellationToken)
        {
            using var conn = new OracleConnection(_connectionString);
            using var cmd = new OracleCommand(sql, conn);
            cmd.BindByName = true;

            cmd.Parameters.Add("id", OracleDbType.Int64).Value = id;
            if (sql.Contains(":reason"))
                cmd.Parameters.Add("reason", OracleDbType.Varchar2).Value = reason ?? "";

            await conn.OpenAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
