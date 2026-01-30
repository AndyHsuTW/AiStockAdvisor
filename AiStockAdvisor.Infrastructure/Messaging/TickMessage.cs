using System;
using System.Globalization;
using System.Text;
using AiStockAdvisor.Domain;

namespace AiStockAdvisor.Infrastructure.Messaging
{
    /// <summary>
    /// 代表發送到 RabbitMQ 的 Tick 訊息格式。
    /// 符合 StockWriter/DbWriter 服務期望的 JSON 結構。
    /// </summary>
    public class TickMessage
    {
        /// <summary>
        /// 交易日期 (YYYY-MM-DD 格式)。
        /// </summary>
        public string TradeDate { get; set; } = string.Empty;

        /// <summary>
        /// 鍵值，格式: {marketNo}-{stockCode}。
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 市場代碼 (1=上市, 2=上櫃)。
        /// </summary>
        public int MarketNo { get; set; }

        /// <summary>
        /// 股票代碼。
        /// </summary>
        public string StockCode { get; set; } = string.Empty;

        /// <summary>
        /// 逐筆序號 (從 1 開始)。
        /// </summary>
        public int SerialNo { get; set; }

        /// <summary>
        /// 成交時間結構。
        /// </summary>
        public TickTimeInfo TickTime { get; set; } = new TickTimeInfo();

        /// <summary>
        /// 買價原始值 (需除以 10000 得實際價格)。
        /// </summary>
        public int BuyPriceRaw { get; set; }

        /// <summary>
        /// 賣價原始值 (需除以 10000 得實際價格)。
        /// </summary>
        public int SellPriceRaw { get; set; }

        /// <summary>
        /// 成交價原始值 (需除以 10000 得實際價格)。
        /// </summary>
        public int DealPriceRaw { get; set; }

        /// <summary>
        /// 成交量。
        /// </summary>
        public int DealVolRaw { get; set; }

        /// <summary>
        /// 內外盤註記 (0=內盤, 1=外盤, ...)。
        /// </summary>
        public int InOutFlag { get; set; }

        /// <summary>
        /// 明細類別 (0=Normal)。
        /// </summary>
        public int TickType { get; set; }

        /// <summary>
        /// 從 Domain Tick 建立 TickMessage。
        /// </summary>
        public static TickMessage FromTick(Tick tick, int buyPriceRaw = 0, int sellPriceRaw = 0, int inOutFlag = 0, int tickType = 0)
        {
            if (tick == null)
                throw new ArgumentNullException(nameof(tick));

            // 將價格轉回原始值 (price * 10000)
            // 根據 stock-tick-response-json-model.md，原始值需除以 10000 得實際價格
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
                    Msec = tick.Time.Millisecond
                },
                BuyPriceRaw = buyPriceRaw,
                SellPriceRaw = sellPriceRaw,
                DealPriceRaw = dealPriceRaw,
                DealVolRaw = (int)tick.Volume,
                InOutFlag = inOutFlag,
                TickType = tickType
            };
        }

        /// <summary>
        /// 將價格轉換為原始值。
        /// 根據規格，原始值 / 10000 = 實際價格，因此 實際價格 * 10000 = 原始值。
        /// 但觀察 run-e2e.sh 範例使用 dealPriceRaw: 22250 表示 222.50，是 * 100。
        /// 為相容現有系統，使用 * 100。
        /// </summary>
        private static int ConvertToRawPrice(decimal price)
        {
            // 使用 * 100，與測試腳本的範例一致
            return (int)(price * 100);
        }

        /// <summary>
        /// 序列化為 JSON 字串。
        /// 使用手動組裝以避免 .NET 4.8 對 System.Text.Json 的相依。
        /// </summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"tradeDate\":\"{0}\"", TradeDate);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"key\":\"{0}\"", EscapeJsonString(Key));
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"marketNo\":{0}", MarketNo);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"stockCode\":\"{0}\"", EscapeJsonString(StockCode));
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"serialNo\":{0}", SerialNo);
            sb.Append(",\"tickTime\":{");
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"hour\":{0}", TickTime.Hour);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"minute\":{0}", TickTime.Minute);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"second\":{0}", TickTime.Second);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"msec\":{0}", TickTime.Msec);
            sb.Append('}');
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"buyPriceRaw\":{0}", BuyPriceRaw);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"sellPriceRaw\":{0}", SellPriceRaw);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"dealPriceRaw\":{0}", DealPriceRaw);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"dealVolRaw\":{0}", DealVolRaw);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"inOutFlag\":{0}", InOutFlag);
            sb.AppendFormat(CultureInfo.InvariantCulture, ",\"tickType\":{0}", TickType);
            sb.Append('}');
            return sb.ToString();
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

    /// <summary>
    /// 成交時間結構。
    /// </summary>
    public class TickTimeInfo
    {
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }
        public int Msec { get; set; }
    }
}
