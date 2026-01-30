using System;
using AiStockAdvisor.Application.Interfaces;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Infrastructure.Messaging;

namespace AiStockAdvisor.Infrastructure.Messaging
{
    /// <summary>
    /// TickPublisher 工廠，用於根據設定建立適當的 ITickPublisher 實例。
    /// </summary>
    public static class TickPublisherFactory
    {
        /// <summary>
        /// 根據設定建立 ITickPublisher 實例。
        /// </summary>
        /// <param name="config">RabbitMQ 設定。若為 null，則從環境變數讀取。</param>
        /// <param name="logger">日誌記錄器。</param>
        /// <returns>ITickPublisher 實例。</returns>
        public static ITickPublisher Create(RabbitMqConfig? config = null, ILogger? logger = null)
        {
            config = config ?? RabbitMqConfig.FromEnvironment();

            if (!config.Enabled)
            {
                logger?.LogInformation("[TickPublisherFactory] RabbitMQ publishing is disabled. Using NullTickPublisher.");
                return new NullTickPublisher();
            }

            try
            {
                return new RabbitMqTickPublisher(config, logger);
            }
            catch (Exception ex)
            {
                logger?.LogError($"[TickPublisherFactory] Failed to create RabbitMqTickPublisher: {ex.Message}", ex);
                logger?.LogWarning("[TickPublisherFactory] Falling back to NullTickPublisher.");
                return new NullTickPublisher();
            }
        }

        /// <summary>
        /// 建立連接到指定主機的 RabbitMQ Publisher。
        /// </summary>
        /// <param name="host">RabbitMQ 主機。</param>
        /// <param name="username">使用者名稱。</param>
        /// <param name="password">密碼。</param>
        /// <param name="logger">日誌記錄器。</param>
        /// <returns>ITickPublisher 實例。</returns>
        public static ITickPublisher Create(
            string host,
            string username,
            string password,
            ILogger? logger = null)
        {
            var config = new RabbitMqConfig
            {
                Host = host,
                Username = username,
                Password = password,
                Enabled = true
            };

            return new RabbitMqTickPublisher(config, logger);
        }
    }
}
