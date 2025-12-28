using System;

namespace AiStockAdvisor.Domain
{
    /// <summary>
    /// 代表市場的逐筆成交資訊 (Tick)。
    /// </summary>
    public class Tick
    {
        /// <summary>
        /// 股票代碼。
        /// </summary>
        public string Symbol { get; }

        /// <summary>
        /// 成交時間。
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// 成交價格。
        /// </summary>
        public decimal Price { get; }

        /// <summary>
        /// 成交單量。
        /// </summary>
        public decimal Volume { get; }

        /// <summary>
        /// 初始化 <see cref="Tick"/> 類別的新執行個體。
        /// </summary>
        /// <param name="symbol">股票代碼。</param>
        /// <param name="time">成交時間。</param>
        /// <param name="price">成交價格。</param>
        /// <param name="volume">成交單量。</param>
        public Tick(string symbol, DateTime time, decimal price, decimal volume)
        {
            Symbol = symbol;
            Time = time;
            Price = price;
            Volume = volume;
        }

        public override string ToString()
        {
            return $"[{Time:HH:mm:ss}] {Symbol} @ {Price} (Vol: {Volume})";
        }
    }
}
