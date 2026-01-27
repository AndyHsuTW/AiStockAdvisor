using System;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Logging;

namespace AiStockAdvisor.Application.Logging
{
    [Obsolete("Use AiStockAdvisor.Logging.LogIdentity instead.")]
    public static class LogContext
    {
        public static LogIdentity ForTick(Tick tick, Guid? traceId = null)
        {
            return LogIdentity.ForTick(tick.TradeDate, tick.MarketNo, tick.Symbol, tick.SerialNo, traceId);
        }

        public static LogIdentity ForNonTick(Guid? traceId = null, string[]? missingFields = null)
        {
            return LogIdentity.ForNonTick(traceId, missingFields);
        }

        public static string BuildFlowId(DateTime tradeDate, int marketNo, string stockCode, int serialNo)
        {
            return LogIdentity.BuildFlowId(tradeDate, marketNo, stockCode, serialNo);
        }
    }
}
