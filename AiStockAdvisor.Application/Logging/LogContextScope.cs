using System;
using AiStockAdvisor.Logging;

namespace AiStockAdvisor.Application.Logging
{
    [Obsolete("Use AiStockAdvisor.Logging.LogScope instead.")]
    public static class LogContextScope
    {
        public static string? CurrentLogId => LogScope.CurrentLogId;

        public static IDisposable Use(string? logId)
        {
            return LogScope.Use(logId);
        }

        public static string FormatMessage(string message)
        {
            return LogScope.FormatMessage(message);
        }
    }
}
