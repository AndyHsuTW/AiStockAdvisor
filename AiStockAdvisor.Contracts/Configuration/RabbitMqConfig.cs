namespace AiStockAdvisor.Contracts.Configuration
{
    /// <summary>
    /// RabbitMQ 連線設定
    /// </summary>
    public class RabbitMqConfig
    {
        public string Host { get; set; } = "192.168.0.43";
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = string.Empty;

        // Exchange 名稱
        public string TickExchange { get; set; } = "stock.ticks";
        public string EventExchange { get; set; } = "trading.events";

        // Queue 名稱
        public string TickQueue { get; set; } = "trading-core.ticks";
    }
}
