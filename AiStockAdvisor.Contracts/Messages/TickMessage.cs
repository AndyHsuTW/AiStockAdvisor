using System;
using AiStockAdvisor.Contracts.Models;

namespace AiStockAdvisor.Contracts.Messages
{
    /// <summary>
    /// RabbitMQ Tick 訊息格式
    /// 符合 Publisher/DbWriter 服務的 JSON 結構
    /// </summary>
    public class TickMessage
    {
        /// <summary>交易日期 (YYYY-MM-DD)</summary>
        public string TradeDate { get; set; } = string.Empty;

        /// <summary>鍵值，格式: {marketNo}-{stockCode}</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>市場代碼 (1=上市, 2=上櫃)</summary>
        public int MarketNo { get; set; }

        /// <summary>股票代碼</summary>
        public string StockCode { get; set; } = string.Empty;

        /// <summary>逐筆序號</summary>
        public int SerialNo { get; set; }

        /// <summary>成交時間結構</summary>
        public TickTimeInfo TickTime { get; set; } = new TickTimeInfo();

        /// <summary>買價原始值 (需除以 10000)</summary>
        public int BuyPriceRaw { get; set; }

        /// <summary>賣價原始值 (需除以 10000)</summary>
        public int SellPriceRaw { get; set; }

        /// <summary>成交價原始值 (需除以 10000)</summary>
        public int DealPriceRaw { get; set; }

        /// <summary>成交量</summary>
        public int DealVolRaw { get; set; }

        /// <summary>內外盤註記 (0=內盤, 1=外盤)</summary>
        public int InOutFlag { get; set; }

        /// <summary>明細類別</summary>
        public int TickType { get; set; }

        public decimal BuyPrice => BuyPriceRaw / 10000m;
        public decimal SellPrice => SellPriceRaw / 10000m;
        public decimal DealPrice => DealPriceRaw / 10000m;

        /// <summary>轉換為 Domain Tick</summary>
        public Tick ToDomainTick()
        {
            var time = new DateTime(
                TickTime.Year, TickTime.Month, TickTime.Day,
                TickTime.Hour, TickTime.Minute, TickTime.Second,
                TickTime.Millisecond);

            return new Tick(
                marketNo: MarketNo,
                symbol: StockCode,
                time: time,
                tradeDate: DateTime.Parse(TradeDate),
                serialNo: SerialNo,
                price: DealPrice,
                volume: DealVolRaw);
        }
    }

    /// <summary>
    /// Tick 時間結構
    /// </summary>
    public class TickTimeInfo
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }
        public int Millisecond { get; set; }
    }
}
