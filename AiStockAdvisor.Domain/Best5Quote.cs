using System;

namespace AiStockAdvisor.Domain
{
    /// <summary>
    /// 表示買賣最佳五檔的即時報價快照。
    /// </summary>
    public sealed class Best5Quote
    {
        public Best5Quote(string symbol, int marketNo, DateTime receivedAt, OrderBookLevel[] bids, OrderBookLevel[] asks)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            MarketNo = marketNo;
            ReceivedAt = receivedAt;
            Bids = bids ?? throw new ArgumentNullException(nameof(bids));
            Asks = asks ?? throw new ArgumentNullException(nameof(asks));

            if (Bids.Length != 5 || Asks.Length != 5)
            {
                throw new ArgumentException("Best5Quote expects exactly 5 bid and 5 ask levels.");
            }
        }

        public string Symbol { get; }
        public int MarketNo { get; }
        public DateTime ReceivedAt { get; }
        public OrderBookLevel[] Bids { get; }
        public OrderBookLevel[] Asks { get; }
    }

    /// <summary>
    /// 單一檔位的價格與數量。
    /// </summary>
    public sealed class OrderBookLevel
    {
        public OrderBookLevel(decimal price, long volume)
        {
            Price = price;
            Volume = volume;
        }

        public decimal Price { get; }
        public long Volume { get; }
    }
}
