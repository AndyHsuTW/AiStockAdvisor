namespace AiStockAdvisor.Infrastructure.Messaging
{
    /// <summary>
    /// RabbitMQ 連線設定。
    /// </summary>
    public class RabbitMqConfig
    {
        /// <summary>
        /// RabbitMQ 主機位址。
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// RabbitMQ 埠號。
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// Virtual Host。
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// 使用者名稱。
        /// </summary>
        public string Username { get; set; } = "guest";

        /// <summary>
        /// 密碼。
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// Exchange 名稱。
        /// </summary>
        public string ExchangeName { get; set; } = "stock-ex";

        /// <summary>
        /// Routing Key。
        /// </summary>
        public string RoutingKey { get; set; } = "stock.twse.tick";

        /// <summary>
        /// 是否啟用發布功能。
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 從環境變數載入設定。
        /// </summary>
        public static RabbitMqConfig FromEnvironment()
        {
            var config = new RabbitMqConfig();

            var host = System.Environment.GetEnvironmentVariable("RABBITMQ_HOST");
            if (!string.IsNullOrWhiteSpace(host))
                config.Host = host;

            var portStr = System.Environment.GetEnvironmentVariable("RABBITMQ_PORT");
            if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var port))
                config.Port = port;

            var vhost = System.Environment.GetEnvironmentVariable("RABBITMQ_VHOST");
            if (!string.IsNullOrWhiteSpace(vhost))
                config.VirtualHost = vhost;

            var user = System.Environment.GetEnvironmentVariable("RABBITMQ_USER");
            if (!string.IsNullOrWhiteSpace(user))
                config.Username = user;

            var pass = System.Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD");
            if (!string.IsNullOrWhiteSpace(pass))
                config.Password = pass;

            var exchange = System.Environment.GetEnvironmentVariable("RABBITMQ_EXCHANGE");
            if (!string.IsNullOrWhiteSpace(exchange))
                config.ExchangeName = exchange;

            var routingKey = System.Environment.GetEnvironmentVariable("RABBITMQ_ROUTING_KEY");
            if (!string.IsNullOrWhiteSpace(routingKey))
                config.RoutingKey = routingKey;

            var enabledStr = System.Environment.GetEnvironmentVariable("RABBITMQ_ENABLED");
            if (!string.IsNullOrWhiteSpace(enabledStr))
                config.Enabled = enabledStr.Equals("true", System.StringComparison.OrdinalIgnoreCase) ||
                                 enabledStr == "1";

            return config;
        }

        /// <summary>
        /// 從 AMQP URI 解析設定。
        /// 格式: amqp://user:pass@host:port/vhost
        /// </summary>
        public static RabbitMqConfig FromUri(string amqpUri)
        {
            var config = new RabbitMqConfig();

            if (string.IsNullOrWhiteSpace(amqpUri))
                return config;

            try
            {
                // 移除 amqp:// 前綴
                var uri = amqpUri.Replace("amqp://", "").Replace("amqps://", "");

                // 分割 vhost
                var parts = uri.Split(new[] { '/' }, 2);
                var hostPart = parts[0];
                if (parts.Length > 1)
                {
                    var vhost = System.Uri.UnescapeDataString(parts[1]);
                    config.VirtualHost = string.IsNullOrEmpty(vhost) ? "/" : vhost;
                }

                // 分割 credentials 和 host
                var atIndex = hostPart.LastIndexOf('@');
                if (atIndex > 0)
                {
                    var credentials = hostPart.Substring(0, atIndex);
                    var hostPort = hostPart.Substring(atIndex + 1);

                    // 解析 credentials
                    var colonIndex = credentials.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        config.Username = credentials.Substring(0, colonIndex);
                        config.Password = credentials.Substring(colonIndex + 1);
                    }

                    // 解析 host:port
                    var portIndex = hostPort.LastIndexOf(':');
                    if (portIndex > 0)
                    {
                        config.Host = hostPort.Substring(0, portIndex);
                        if (int.TryParse(hostPort.Substring(portIndex + 1), out var port))
                            config.Port = port;
                    }
                    else
                    {
                        config.Host = hostPort;
                    }
                }
            }
            catch
            {
                // 解析失敗，返回預設值
            }

            return config;
        }
    }
}
