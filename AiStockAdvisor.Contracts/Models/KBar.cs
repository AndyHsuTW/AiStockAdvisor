using System;

namespace AiStockAdvisor.Contracts.Models
{
    /// <summary>
    /// 代表一根 K 線 (K-Bar)，包含特定時間區間內的 OHLCV 資訊
    /// .NET Standard 2.0 相容版本
    /// </summary>
    public class KBar
    {
        /// <summary>K 線區間的結束時間</summary>
        public DateTime Time { get; }

        /// <summary>開盤價</summary>
        public decimal Open { get; }

        /// <summary>最高價</summary>
        public decimal High { get; }

        /// <summary>最低價</summary>
        public decimal Low { get; }

        /// <summary>收盤價</summary>
        public decimal Close { get; }

        /// <summary>成交量</summary>
        public decimal Volume { get; }

        public KBar(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            if (high < low)
                throw new ArgumentException("High price cannot be less than Low price");

            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        /// <summary>實體長度（絕對值）</summary>
        public decimal Body => Math.Abs(Close - Open);

        /// <summary>上影線長度</summary>
        public decimal UpperShadow => High - Math.Max(Open, Close);

        /// <summary>下影線長度</summary>
        public decimal LowerShadow => Math.Min(Open, Close) - Low;

        /// <summary>是否為陽線</summary>
        public bool IsBullish => Close > Open;

        /// <summary>是否為陰線</summary>
        public bool IsBearish => Close < Open;
    }
}
