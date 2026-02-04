using System;

namespace AiStockAdvisor.Contracts.Models
{
    /// <summary>
    /// 代表市場的逐筆成交資訊 (Tick)。
    /// .NET Standard 2.0 相容版本
    /// </summary>
    public class Tick
    {
        /// <summary>市場代碼 (1=上市, 2=上櫃)</summary>
        public int MarketNo { get; }

        /// <summary>股票代碼</summary>
        public string Symbol { get; }

        /// <summary>成交時間</summary>
        public DateTime Time { get; }

        /// <summary>交易日期</summary>
        public DateTime TradeDate { get; }

        /// <summary>逐筆序號</summary>
        public int SerialNo { get; }

        /// <summary>成交價格</summary>
        public decimal Price { get; }

        /// <summary>成交單量（張）</summary>
        public decimal Volume { get; }

        public Tick(int marketNo, string symbol, DateTime time, DateTime tradeDate, int serialNo, decimal price, decimal volume)
        {
            MarketNo = marketNo;
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Time = time;
            TradeDate = tradeDate;
            SerialNo = serialNo;
            Price = price;
            Volume = volume;
        }

        /// <summary>簡化建構函式</summary>
        public Tick(string symbol, DateTime time, decimal price, decimal volume)
            : this(0, symbol, time, time.Date, 0, price, volume)
        {
        }

        public override string ToString()
            => $"[{Time:HH:mm:ss}] {Symbol} @ {Price} (Vol: {Volume})";
    }
}
