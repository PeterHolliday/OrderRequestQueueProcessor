using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;

namespace OrderRequestQueueProcessor.Logging
{
    public static class LogExtensions
    {
        public static void LogInfo(this ILogger logger, string module, string msg, params object[] args)
        {
            using (LogContext.PushProperty("Module", module))
            using (LogContext.PushProperty("Type", "Info"))
            {
                logger.LogInformation(msg, args);
            }
        }

        public static void LogInfo(this ILogger logger, string module, Exception ex, string msg, params object[] args)
        {
            using (LogContext.PushProperty("Module", module))
            using (LogContext.PushProperty("Type", "Info"))
            {
                logger.LogInformation(ex, msg, args);
            }
        }

        public static void LogSuccess(this ILogger logger, string module, string msg, params object[] args)
        {
            using (LogContext.PushProperty("Module", module))
            using (LogContext.PushProperty("Type", "Success"))
            {
                logger.LogInformation(msg, args);
            }
        }

        public static void LogFailure(this ILogger logger, string module, string msg, params object[] args)
        {
            using (LogContext.PushProperty("Module", module))
            using (LogContext.PushProperty("Type", "Failure"))
            {
                logger.LogError(msg, args);
            }
        }

        public static void LogFailure(this ILogger logger, string module, Exception ex, string msg, params object[] args)
        {
            using (LogContext.PushProperty("Module", module))
            using (LogContext.PushProperty("Type", "Failure"))
            {
                logger.LogError(ex, msg, args);
            }
        }
    }
}
