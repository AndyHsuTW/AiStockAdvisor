using System;
using System.Collections.Generic;
using System.Text;

namespace AiStockAdvisor.Logging
{
    public sealed class LogIdentity
    {
        public string? LogId { get; }
        public string? TraceId { get; }
        public DateTime? TradeDate { get; }
        public int? MarketNo { get; }
        public string? StockCode { get; }
        public int? SerialNo { get; }
        public string? FlowId { get; }
        public string[]? MissingFields { get; }

        private LogIdentity(
            string? logId,
            string? traceId,
            DateTime? tradeDate,
            int? marketNo,
            string? stockCode,
            int? serialNo,
            string? flowId,
            string[]? missingFields)
        {
            LogId = logId;
            TraceId = traceId;
            TradeDate = tradeDate;
            MarketNo = marketNo;
            StockCode = stockCode;
            SerialNo = serialNo;
            FlowId = flowId;
            MissingFields = missingFields;
        }

        public static LogIdentity ForTick(
            DateTime tradeDate,
            int marketNo,
            string stockCode,
            int serialNo,
            Guid? traceId = null,
            string[]? missingFields = null)
        {
            var flowId = BuildFlowId(tradeDate, marketNo, stockCode, serialNo);
            return new LogIdentity(
                logId: null,
                traceId: traceId?.ToString(),
                tradeDate: tradeDate.Date,
                marketNo: marketNo,
                stockCode: stockCode,
                serialNo: serialNo,
                flowId: flowId,
                missingFields: missingFields);
        }

        public static LogIdentity ForNonTick(Guid? traceId = null, string[]? missingFields = null)
        {
            return new LogIdentity(
                logId: Guid.NewGuid().ToString(),
                traceId: (traceId ?? Guid.NewGuid()).ToString(),
                tradeDate: null,
                marketNo: null,
                stockCode: null,
                serialNo: null,
                flowId: null,
                missingFields: missingFields);
        }

        public static string BuildFlowId(DateTime tradeDate, int marketNo, string stockCode, int serialNo)
        {
            return $"{tradeDate:yyyy-MM-dd}-{marketNo}-{stockCode}-{serialNo}";
        }

        public string ToJson()
        {
            var items = new List<string>(8);
            if (!string.IsNullOrWhiteSpace(LogId))
            {
                items.Add($"\"logId\":\"{EscapeJson(LogId)}\"");
            }

            if (!string.IsNullOrWhiteSpace(TraceId))
            {
                items.Add($"\"traceId\":\"{EscapeJson(TraceId)}\"");
            }

            if (TradeDate.HasValue)
            {
                items.Add($"\"tradeDate\":\"{TradeDate:yyyy-MM-dd}\"");
            }

            if (MarketNo.HasValue)
            {
                items.Add($"\"marketNo\":{MarketNo.Value}");
            }

            if (!string.IsNullOrWhiteSpace(StockCode))
            {
                items.Add($"\"stockCode\":\"{EscapeJson(StockCode)}\"");
            }

            if (SerialNo.HasValue)
            {
                items.Add($"\"serialNo\":{SerialNo.Value}");
            }

            if (!string.IsNullOrWhiteSpace(FlowId))
            {
                items.Add($"\"flowId\":\"{EscapeJson(FlowId)}\"");
            }

            if (MissingFields != null && MissingFields.Length > 0)
            {
                var sb = new StringBuilder();
                sb.Append("\"missingFields\":[");
                for (int i = 0; i < MissingFields.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(EscapeJson(MissingFields[i])).Append('"');
                }

                sb.Append(']');
                items.Add(sb.ToString());
            }

            return "{" + string.Join(",", items) + "}";
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
