using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using AiStockAdvisor.Application.Interfaces;
using AiStockAdvisor.Application.Models;
using AiStockAdvisor.Logging;

namespace AiStockAdvisor.Infrastructure.Notifications
{
    /// <summary>
    /// 使用 LINE Messaging API 發送缺號通知。
    /// </summary>
    public sealed class LineAlertService : IGapAlertService, IDisposable
    {
        private const string LinePushApiUrl = "https://api.line.me/v2/bot/message/push";
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly bool _enabled;
        private readonly string? _channelAccessToken;
        private readonly string? _userId;
        private readonly TimeSpan _cooldown;
        private readonly ConcurrentDictionary<string, DateTime> _lastSuccessUtcByCategory
            = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 初始化 <see cref="LineAlertService"/> 類別的新執行個體。
        /// </summary>
        /// <param name="logger">日誌服務。</param>
        /// <param name="httpClient">HTTP 客戶端；若為 null 則建立內部實例。</param>
        public LineAlertService(ILogger logger, HttpClient? httpClient = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? new HttpClient();
            _ownsHttpClient = httpClient == null;
            _httpClient.Timeout = TimeSpan.FromSeconds(8);

            _enabled = ReadBooleanEnv("ENABLE_GAP_ALERT", true);
            _cooldown = TimeSpan.FromSeconds(ReadIntEnv("ALERT_COOLDOWN_SECONDS", 60, minValue: 0, maxValue: 3600));
            _channelAccessToken = ReadEnv("LINE_CHANNEL_ACCESS_TOKEN");
            _userId = ReadEnv("LINE_USER_ID");

            _logger.LogInformation(LogScope.FormatMessage(
                $"[LineAlert] Initialized. enabled={_enabled}, tokenSet={!string.IsNullOrWhiteSpace(_channelAccessToken)}, userIdSet={!string.IsNullOrWhiteSpace(_userId)}, cooldownSec={(int)_cooldown.TotalSeconds}"));
        }

        /// <summary>
        /// 發送 SerialNo 缺號通知。
        /// </summary>
        /// <param name="gapEvent">缺號事件資料。</param>
        public void NotifySerialNoGap(SerialNoGapEvent gapEvent)
        {
            if (gapEvent == null) throw new ArgumentNullException(nameof(gapEvent));

            var category = NormalizeCategory(gapEvent.StockCode);

            if (!_enabled)
            {
                _logger.LogInformation(LogScope.FormatMessage(
                    $"[LineAlert] Skip send. reason=disabled, category={category}, missing={gapEvent.MissingStartSerialNo}-{gapEvent.MissingEndSerialNo}"));
                return;
            }

            if (string.IsNullOrWhiteSpace(_channelAccessToken) || string.IsNullOrWhiteSpace(_userId))
            {
                _logger.LogWarning(LogScope.FormatMessage(
                    $"[LineAlert] Skip send. reason=missing-config, tokenSet={!string.IsNullOrWhiteSpace(_channelAccessToken)}, userIdSet={!string.IsNullOrWhiteSpace(_userId)}, category={category}"));
                return;
            }

            var channelAccessToken = _channelAccessToken!;
            var userId = _userId!;

            var nowUtc = DateTime.UtcNow;
            if (_lastSuccessUtcByCategory.TryGetValue(category, out var lastSuccessUtc))
            {
                var elapsed = nowUtc - lastSuccessUtc;
                if (elapsed < _cooldown)
                {
                    _logger.LogInformation(LogScope.FormatMessage(
                        $"[LineAlert] Skip send. reason=cooldown, category={category}, remainingSec={(int)(_cooldown - elapsed).TotalSeconds}"));
                    return;
                }
            }

            var message = BuildMessage(gapEvent);
            var payload = BuildPushPayload(userId, message);

            try
            {
                _logger.LogInformation(LogScope.FormatMessage(
                    $"[LineAlert] Sending. category={category}, stock={gapEvent.StockCode}, prev={gapEvent.PreviousSerialNo}, curr={gapEvent.CurrentSerialNo}, missing={gapEvent.MissingStartSerialNo}-{gapEvent.MissingEndSerialNo}"));

                using (var request = new HttpRequestMessage(HttpMethod.Post, LinePushApiUrl))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken);
                    request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                    using (var response = _httpClient.SendAsync(request).GetAwaiter().GetResult())
                    {
                        var responseBody = response.Content == null
                            ? string.Empty
                            : response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var responsePreview = Truncate(responseBody, 200);

                        if (response.IsSuccessStatusCode)
                        {
                            _lastSuccessUtcByCategory[category] = nowUtc;
                            _logger.LogInformation(LogScope.FormatMessage(
                                $"[LineAlert] Sent success. category={category}, status={(int)response.StatusCode}, body={responsePreview}"));
                            return;
                        }

                        _logger.LogError(LogScope.FormatMessage(
                            $"[LineAlert] Sent failed. category={category}, status={(int)response.StatusCode}, body={responsePreview}"));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(LogScope.FormatMessage(
                    $"[LineAlert] Send exception. category={category}"), ex);
            }
        }

        /// <summary>
        /// 釋放資源。
        /// </summary>
        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }

        /// <summary>
        /// 組合 LINE 文字訊息。
        /// </summary>
        /// <param name="gapEvent">缺號事件資料。</param>
        /// <returns>通知文字內容。</returns>
        private static string BuildMessage(SerialNoGapEvent gapEvent)
        {
            return
                "[SerialNoGap]\n" +
                $"stock={gapEvent.StockCode}\n" +
                $"prev={gapEvent.PreviousSerialNo}, curr={gapEvent.CurrentSerialNo}\n" +
                $"missing={gapEvent.MissingStartSerialNo}-{gapEvent.MissingEndSerialNo} ({gapEvent.MissingCount})\n" +
                $"tickTime={gapEvent.TickTime:yyyy-MM-dd HH:mm:ss.fff}\n" +
                $"detectAt={gapEvent.DetectedAt:yyyy-MM-dd HH:mm:ss.fff}";
        }

        /// <summary>
        /// 建立 LINE Push API JSON payload。
        /// </summary>
        /// <param name="userId">接收者使用者 ID。</param>
        /// <param name="message">通知訊息。</param>
        /// <returns>JSON 字串。</returns>
        private static string BuildPushPayload(string userId, string message)
        {
            var escapedUserId = EscapeJson(userId);
            var escapedMessage = EscapeJson(message);
            return $"{{\"to\":\"{escapedUserId}\",\"messages\":[{{\"type\":\"text\",\"text\":\"{escapedMessage}\"}}]}}";
        }

        /// <summary>
        /// 將字串做最小 JSON 跳脫。
        /// </summary>
        /// <param name="value">原始字串。</param>
        /// <returns>跳脫後字串。</returns>
        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// 將分類鍵正規化。
        /// </summary>
        /// <param name="category">分類鍵。</param>
        /// <returns>正規化結果。</returns>
        private static string NormalizeCategory(string category)
        {
            return string.IsNullOrWhiteSpace(category) ? "UNKNOWN" : category.Trim();
        }

        /// <summary>
        /// 讀取字串環境變數。
        /// </summary>
        /// <param name="key">環境變數名稱。</param>
        /// <returns>變數值。</returns>
        private static string? ReadEnv(string key)
        {
            return Environment.GetEnvironmentVariable(key);
        }

        /// <summary>
        /// 讀取布林環境變數。
        /// </summary>
        /// <param name="key">環境變數名稱。</param>
        /// <param name="defaultValue">預設值。</param>
        /// <returns>解析結果。</returns>
        private static bool ReadBooleanEnv(string key, bool defaultValue)
        {
            var raw = ReadEnv(key);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            if (bool.TryParse(raw, out var parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        /// <summary>
        /// 讀取整數環境變數，並限制範圍。
        /// </summary>
        /// <param name="key">環境變數名稱。</param>
        /// <param name="defaultValue">預設值。</param>
        /// <param name="minValue">最小值。</param>
        /// <param name="maxValue">最大值。</param>
        /// <returns>解析與限制後的整數值。</returns>
        private static int ReadIntEnv(string key, int defaultValue, int minValue, int maxValue)
        {
            var raw = ReadEnv(key);
            if (string.IsNullOrWhiteSpace(raw) || !int.TryParse(raw, out var value))
            {
                return defaultValue;
            }

            if (value < minValue)
            {
                return minValue;
            }

            if (value > maxValue)
            {
                return maxValue;
            }

            return value;
        }

        /// <summary>
        /// 截斷字串至指定長度。
        /// </summary>
        /// <param name="value">原始字串。</param>
        /// <param name="maxLength">最大長度。</param>
        /// <returns>截斷後字串。</returns>
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength) + "...";
        }
    }
}
