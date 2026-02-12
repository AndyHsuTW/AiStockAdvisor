using System;
using System.Collections.Generic;
using System.Text;

namespace AiStockAdvisor.Infrastructure.Messaging
{
    /// <summary>
    /// Publisher 啟動時的防呆檢查，禁止注入任何 consume 相關設定。
    /// </summary>
    public static class PublisherConsumeGuard
    {
        private const string WorkQueueName = "work-queue";

        private static readonly string[] ForbiddenConsumeQueueKeys =
        {
            "RABBITMQ_CONSUME_QUEUE",
            "RABBITMQ_CONSUMER_QUEUE",
            "RABBITMQ_READ_QUEUE",
            "CONSUME_QUEUE",
            "CONSUMER_QUEUE"
        };

        private static readonly string[] ForbiddenLegacyQueueKeys =
        {
            "QUEUE_NAME",
            "RABBITMQ_QUEUE",
            "RABBITMQ_QUEUE_NAME",
            "WORK_QUEUE",
            "RABBITMQ_WORK_QUEUE",
            "TICK_QUEUE",
            "RABBITMQ_TICK_QUEUE"
        };

        /// <summary>
        /// 驗證 publisher 啟動環境，若偵測到 consume 相關設定則直接失敗。
        /// </summary>
        public static void EnsureNoConsumeConfiguration()
        {
            var violations = new List<string>();

            for (var i = 0; i < ForbiddenConsumeQueueKeys.Length; i++)
            {
                var key = ForbiddenConsumeQueueKeys[i];
                var value = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    violations.Add($"{key}={value}");
                }
            }

            for (var i = 0; i < ForbiddenLegacyQueueKeys.Length; i++)
            {
                var key = ForbiddenLegacyQueueKeys[i];
                var value = Environment.GetEnvironmentVariable(key);
                if (string.Equals(value, WorkQueueName, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{key}={value}");
                }
            }

            var consumeEnabled = Environment.GetEnvironmentVariable("RABBITMQ_CONSUME_ENABLED");
            if (IsTrue(consumeEnabled))
            {
                violations.Add($"RABBITMQ_CONSUME_ENABLED={consumeEnabled}");
            }

            if (violations.Count == 0)
            {
                return;
            }

            var message = new StringBuilder();
            message.Append("[PublisherConsumeGuard] Publisher 偵測到 consume 設定，依規範拒絕啟動。");
            message.Append(" 請移除 consume 參數，並確認 stock-publisher 僅有 publish 權限。");
            message.Append(" Violations: ");
            message.Append(string.Join(", ", violations));

            throw new InvalidOperationException(message.ToString());
        }

        /// <summary>
        /// 判斷環境變數是否為啟用狀態。
        /// </summary>
        /// <param name="value">環境變數值。</param>
        /// <returns>若為 true/1/yes/on 則回傳 true。</returns>
        private static bool IsTrue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }
    }
}
