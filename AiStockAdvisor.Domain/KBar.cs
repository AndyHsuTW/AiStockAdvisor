using System;

namespace AiStockAdvisor.Domain
{
    /// <summary>
    /// 代表一根 K 線 (K-Bar) 紀錄，包含特定時間區間內的價格與成交量資訊。
    /// 數值包含該時段的完整資訊。
    /// </summary>
    public class KBar
    {
        /// <summary>
        /// 取得 K 線區間的結束時間。
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// 取得該區間的開盤價。
        /// </summary>
        public decimal Open { get; }

        /// <summary>
        /// 取得該區間的最高價。
        /// </summary>
        public decimal High { get; }

        /// <summary>
        /// 取得該區間的最低價。
        /// </summary>
        public decimal Low { get; }

        /// <summary>
        /// 取得該區間的收盤價。
        /// </summary>
        public decimal Close { get; }

        /// <summary>
        /// 取得該區間的成交量。
        /// </summary>
        public decimal Volume { get; }

        /// <summary>
        /// 初始化 <see cref="KBar"/> 類別的新執行個體。
        /// </summary>
        /// <param name="time">K 線的結束時間。</param>
        /// <param name="open">開盤價。</param>
        /// <param name="high">最高價。</param>
        /// <param name="low">最低價。</param>
        /// <param name="close">收盤價。</param>
        /// <param name="volume">成交量。</param>
        /// <exception cref="ArgumentException">當最高價低於最低價時拋出此例外。</exception>
        public KBar(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            if (high < low)
                throw new ArgumentException("High price cannot be lower than Low price.");

            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
    }
}
