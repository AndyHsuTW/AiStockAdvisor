using System;
using System.Text;
using AiStockAdvisor.Application.Interfaces;
using AiStockAdvisor.Domain;
using RabbitMQ.Client;
using ILogger = AiStockAdvisor.Application.Interfaces.ILogger;

namespace AiStockAdvisor.Infrastructure.Messaging
{
    /// <summary>
    /// 透過 RabbitMQ 發布 Tick 資料的實作。
    /// </summary>
    public class RabbitMqTickPublisher : ITickPublisher, IDisposable
    {
        private readonly RabbitMqConfig _config;
        private readonly ILogger? _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// 初始化 RabbitMqTickPublisher。
        /// </summary>
        /// <param name="config">RabbitMQ 設定。</param>
        /// <param name="logger">日誌記錄器 (可選)。</param>
        public RabbitMqTickPublisher(RabbitMqConfig config, ILogger? logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;

            if (_config.Enabled)
            {
                Connect();
            }
        }

        private void Connect()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config.Host,
                    Port = _config.Port,
                    VirtualHost = _config.VirtualHost,
                    UserName = _config.Username,
                    Password = _config.Password,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // 宣告 Exchange (如果不存在)
                _channel.ExchangeDeclare(
                    exchange: _config.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    arguments: null);

                _logger?.LogInformation($"[RabbitMqTickPublisher] Connected to {_config.Host}:{_config.Port}, exchange: {_config.ExchangeName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[RabbitMqTickPublisher] Failed to connect to RabbitMQ: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public void Publish(Tick tick)
        {
            Publish(tick, 0, 0, 0, 0);
        }

        /// <inheritdoc />
        public void Publish(Tick tick, int buyPriceRaw, int sellPriceRaw, int inOutFlag, int tickType)
        {
            if (!_config.Enabled)
                return;

            if (tick == null)
            {
                _logger?.LogError("[RabbitMqTickPublisher] Tick is null, skipping publish.");
                return;
            }

            try
            {
                EnsureConnected();

                if (_channel == null || !_channel.IsOpen)
                {
                    _logger?.LogError("[RabbitMqTickPublisher] Channel is not available, skipping publish.");
                    return;
                }

                var message = TickMessage.FromTick(tick, buyPriceRaw, sellPriceRaw, inOutFlag, tickType);
                var json = message.ToJson();
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.ContentType = "application/json";
                properties.ContentEncoding = "utf-8";
                properties.DeliveryMode = 2; // Persistent
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                lock (_lock)
                {
                    _channel.BasicPublish(
                        exchange: _config.ExchangeName,
                        routingKey: _config.RoutingKey,
                        basicProperties: properties,
                        body: body);
                }

                _logger?.LogInformation($"[RabbitMqTickPublisher] Published: {tick.Symbol} @ {tick.Price}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[RabbitMqTickPublisher] Failed to publish tick: {ex.Message}", ex);
            }
        }

        private void EnsureConnected()
        {
            if (_connection == null || !_connection.IsOpen || _channel == null || !_channel.IsOpen)
            {
                lock (_lock)
                {
                    if (_connection == null || !_connection.IsOpen || _channel == null || !_channel.IsOpen)
                    {
                        CleanupConnection();
                        Connect();
                    }
                }
            }
        }

        private void CleanupConnection()
        {
            try
            {
                _channel?.Close();
                _channel?.Dispose();
            }
            catch { }

            try
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            catch { }

            _channel = null;
            _connection = null;
        }

        /// <inheritdoc />
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// 釋放資源。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            lock (_lock)
            {
                CleanupConnection();
            }

            _logger?.LogInformation("[RabbitMqTickPublisher] Disposed.");
        }
    }
}
