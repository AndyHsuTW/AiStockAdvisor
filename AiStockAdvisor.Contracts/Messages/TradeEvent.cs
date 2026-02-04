using System;

namespace AiStockAdvisor.Contracts.Messages
{
    /// <summary>
    /// 交易事件訊息（發布至 RabbitMQ）
    /// </summary>
    public class TradeEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;  // OrderCreated, OrderFilled, PositionOpened, PositionClosed
        public DateTime Timestamp { get; set; }

        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;       // Buy, Sell
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public string TriggerRule { get; set; } = string.Empty;
        public string ExecutionMode { get; set; } = string.Empty;  // Simulated, LineNotify, Real

        /// <summary>附加資訊 (JSON 格式)</summary>
        public string Metadata { get; set; } = string.Empty;
    }
}
