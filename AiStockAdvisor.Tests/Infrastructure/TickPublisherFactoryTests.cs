using System;
using System.Collections.Generic;
using AiStockAdvisor.Infrastructure.Messaging;
using Xunit;

namespace AiStockAdvisor.Tests.Infrastructure
{
    /// <summary>
    /// 驗證 TickPublisherFactory 啟動防護邏輯。
    /// </summary>
    public class TickPublisherFactoryTests
    {
        /// <summary>
        /// 若注入 consume queue 設定，應 fail-fast 拒絕啟動。
        /// </summary>
        [Fact]
        public void Create_WhenConsumeQueueConfigured_ShouldThrow()
        {
            WithEnvironmentVariables(
                new Dictionary<string, string?>
                {
                    ["RABBITMQ_CONSUME_QUEUE"] = "work-queue",
                    ["QUEUE_NAME"] = null
                },
                () =>
                {
                    var config = new RabbitMqConfig { Enabled = false };
                    var ex = Assert.Throws<InvalidOperationException>(() => TickPublisherFactory.Create(config));

                    Assert.Contains("RABBITMQ_CONSUME_QUEUE", ex.Message);
                });
        }

        /// <summary>
        /// 若 legacy queue 參數指向 work-queue，應 fail-fast 拒絕啟動。
        /// </summary>
        [Fact]
        public void Create_WhenLegacyQueueTargetsWorkQueue_ShouldThrow()
        {
            WithEnvironmentVariables(
                new Dictionary<string, string?>
                {
                    ["RABBITMQ_CONSUME_QUEUE"] = null,
                    ["QUEUE_NAME"] = "work-queue"
                },
                () =>
                {
                    var config = new RabbitMqConfig { Enabled = false };
                    var ex = Assert.Throws<InvalidOperationException>(() => TickPublisherFactory.Create(config));

                    Assert.Contains("QUEUE_NAME=work-queue", ex.Message);
                });
        }

        /// <summary>
        /// 無 consume 設定且 publish 關閉時，應回傳 NullTickPublisher。
        /// </summary>
        [Fact]
        public void Create_WhenDisabledAndNoConsumeConfig_ShouldReturnNullTickPublisher()
        {
            WithEnvironmentVariables(
                new Dictionary<string, string?>
                {
                    ["RABBITMQ_CONSUME_QUEUE"] = null,
                    ["QUEUE_NAME"] = null,
                    ["RABBITMQ_CONSUME_ENABLED"] = null
                },
                () =>
                {
                    var config = new RabbitMqConfig { Enabled = false };
                    var publisher = TickPublisherFactory.Create(config);

                    Assert.IsType<NullTickPublisher>(publisher);
                });
        }

        /// <summary>
        /// 在測試期間暫時覆寫多個環境變數，結束後還原。
        /// </summary>
        /// <param name="variables">要覆寫的環境變數集合。</param>
        /// <param name="action">測試動作。</param>
        private static void WithEnvironmentVariables(
            IDictionary<string, string?> variables,
            Action action)
        {
            var originals = new Dictionary<string, string?>();
            try
            {
                foreach (var pair in variables)
                {
                    originals[pair.Key] = Environment.GetEnvironmentVariable(pair.Key);
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                }

                action();
            }
            finally
            {
                foreach (var pair in originals)
                {
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
                }
            }
        }
    }
}
