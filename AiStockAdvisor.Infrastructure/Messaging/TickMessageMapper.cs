using System;
using System.Globalization;
using System.Text;
using AiStockAdvisor.Contracts.Messages;
using AiStockAdvisor.Domain;

namespace AiStockAdvisor.Infrastructure.Messaging
{
    public static class TickMessageMapper
    {
        public static TickMessage FromTick(Tick tick, int buyPriceRaw = 0, int sellPriceRaw = 0, int inOutFlag = 0, int tickType = 0)
        {
            if (tick == null)
                throw new ArgumentNullException(nameof(tick));

            int dealPriceRaw = ConvertToRawPrice(tick.Price);

            return new TickMessage
            {
                TradeDate = tick.TradeDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Key = $"{tick.MarketNo}-{tick.Symbol}",
                MarketNo = tick.MarketNo,
                StockCode = tick.Symbol,
                SerialNo = tick.SerialNo,
                TickTime = new TickTimeInfo
                {
                    Hour = tick.Time.Hour,
                    Minute = tick.Time.Minute,
                    Second = tick.Time.Second,
                    Millisecond = tick.Time.Millisecond
                },
                BuyPriceRaw = buyPriceRaw,
                SellPriceRaw = sellPriceRaw,
                DealPriceRaw = dealPriceRaw,
                DealVolRaw = (int)tick.Volume,
                InOutFlag = inOutFlag,
                TickType = tickType
            };
        }

        public static string ToJson(TickMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var sb = new StringBuilder();
            sb.Append('{');
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"tradeDate\":\"{0}\"", message.TradeDate);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"key\":\"{0}\"", EscapeJsonString(message.Key));
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"marketNo\":{0}", message.MarketNo);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"stockCode\":\"{0}\"", EscapeJsonString(message.StockCode));
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"serialNo\":{0}", message.SerialNo);
            sb.Append(",\"tickTime\":{");
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"hour\":{0}", message.TickTime.Hour);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"minute\":{0}", message.TickTime.Minute);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"second\":{0}", message.TickTime.Second);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"msec\":{0}", message.TickTime.Millisecond);
            sb.Append('}');
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"buyPriceRaw\":{0}", message.BuyPriceRaw);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"sellPriceRaw\":{0}", message.SellPriceRaw);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"dealPriceRaw\":{0}", message.DealPriceRaw);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"dealVolRaw\":{0}", message.DealVolRaw);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"inOutFlag\":{0}", message.InOutFlag);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"tickType\":{0}", message.TickType);
            sb.Append('}');
            return sb.ToString();
        }

        private static int ConvertToRawPrice(decimal price)
        {
            return (int)(price * 100);
        }

        private static string EscapeJsonString(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
