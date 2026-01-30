using System;
using System.Globalization;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Infrastructure.Messaging;
using Xunit;

namespace AiStockAdvisor.Tests.Infrastructure
{
    /// <summary>
    /// TickMessage 類別的單元測試。
    /// </summary>
    public class TickMessageTests
    {
        [Fact]
        public void FromTick_ShouldCreateCorrectMessage()
        {
            // Arrange
            var tradeDate = new DateTime(2026, 1, 30);
            var tickTime = tradeDate.AddHours(9).AddMinutes(30).AddSeconds(15).AddMilliseconds(123);
            var tick = new Tick("2330", tickTime, 222.50m, 100, 1, 12345, tradeDate);

            // Act
            var message = TickMessage.FromTick(tick);

            // Assert
            Assert.Equal("2026-01-30", message.TradeDate);
            Assert.Equal("1-2330", message.Key);
            Assert.Equal(1, message.MarketNo);
            Assert.Equal("2330", message.StockCode);
            Assert.Equal(12345, message.SerialNo);
            Assert.Equal(9, message.TickTime.Hour);
            Assert.Equal(30, message.TickTime.Minute);
            Assert.Equal(15, message.TickTime.Second);
            Assert.Equal(123, message.TickTime.Msec);
            Assert.Equal(22250, message.DealPriceRaw); // 222.50 * 100
            Assert.Equal(100, message.DealVolRaw);
        }

        [Fact]
        public void FromTick_WithRawPrices_ShouldIncludeAllFields()
        {
            // Arrange
            var tradeDate = new DateTime(2026, 1, 30);
            var tickTime = tradeDate.AddHours(10);
            var tick = new Tick("2327", tickTime, 150.00m, 50, 2, 1, tradeDate);

            // Act
            var message = TickMessage.FromTick(tick, buyPriceRaw: 14990, sellPriceRaw: 15010, inOutFlag: 1, tickType: 0);

            // Assert
            Assert.Equal(14990, message.BuyPriceRaw);
            Assert.Equal(15010, message.SellPriceRaw);
            Assert.Equal(1, message.InOutFlag);
            Assert.Equal(0, message.TickType);
        }

        [Fact]
        public void ToJson_ShouldProduceValidJsonFormat()
        {
            // Arrange
            var tradeDate = new DateTime(2026, 1, 30);
            var tickTime = tradeDate.AddHours(9).AddMinutes(0).AddSeconds(1).AddMilliseconds(123);
            var tick = new Tick("2330", tickTime, 222.50m, 1, 1, 1, tradeDate);
            var message = TickMessage.FromTick(tick, buyPriceRaw: 22250, sellPriceRaw: 22260, inOutFlag: 0, tickType: 0);

            // Act
            var json = message.ToJson();

            // Assert - 驗證 JSON 格式符合接收服務期望
            Assert.Contains("\"tradeDate\":\"2026-01-30\"", json);
            Assert.Contains("\"key\":\"1-2330\"", json);
            Assert.Contains("\"marketNo\":1", json);
            Assert.Contains("\"stockCode\":\"2330\"", json);
            Assert.Contains("\"serialNo\":1", json);
            Assert.Contains("\"tickTime\":{", json);
            Assert.Contains("\"hour\":9", json);
            Assert.Contains("\"minute\":0", json);
            Assert.Contains("\"second\":1", json);
            Assert.Contains("\"msec\":123", json);
            Assert.Contains("\"buyPriceRaw\":22250", json);
            Assert.Contains("\"sellPriceRaw\":22260", json);
            Assert.Contains("\"dealPriceRaw\":22250", json);
            Assert.Contains("\"dealVolRaw\":1", json);
            Assert.Contains("\"inOutFlag\":0", json);
            Assert.Contains("\"tickType\":0", json);
        }

        [Fact]
        public void ToJson_ShouldMatchE2ETestPayloadFormat()
        {
            // Arrange - 使用 run-e2e.sh 中的範例資料
            var tradeDate = DateTime.Parse("2026-01-30");
            var tickTime = tradeDate.AddHours(9).AddMinutes(0).AddSeconds(1).AddMilliseconds(123);
            var tick = new Tick("2330", tickTime, 222.50m, 1, 1, 1, tradeDate);
            var message = TickMessage.FromTick(tick, buyPriceRaw: 22250, sellPriceRaw: 22260, inOutFlag: 0, tickType: 0);

            // Act
            var json = message.ToJson();

            // Assert - 驗證完整 JSON 結構
            Assert.StartsWith("{", json);
            Assert.EndsWith("}", json);
            
            // 確保沒有多餘的空格或換行
            Assert.DoesNotContain("\n", json);
            Assert.DoesNotContain("\r", json);
        }

        [Fact]
        public void ToJson_ShouldEscapeSpecialCharacters()
        {
            // Arrange
            var tradeDate = new DateTime(2026, 1, 30);
            var tickTime = tradeDate.AddHours(10);
            // 使用包含特殊字元的股票代碼（雖然實際不太可能）
            var tick = new Tick("TEST\"SYMBOL", tickTime, 100.00m, 10, 1, 1, tradeDate);

            // Act
            var message = TickMessage.FromTick(tick);
            var json = message.ToJson();

            // Assert - 雙引號應該被跳脫
            Assert.Contains("\\\"", json);
        }
    }

    /// <summary>
    /// RabbitMqConfig 類別的單元測試。
    /// </summary>
    public class RabbitMqConfigTests
    {
        [Fact]
        public void FromUri_ShouldParseAmqpUri()
        {
            // Arrange
            var uri = "amqp://stock-publisher:love0521@192.168.0.43:5672/%2F";

            // Act
            var config = RabbitMqConfig.FromUri(uri);

            // Assert
            Assert.Equal("192.168.0.43", config.Host);
            Assert.Equal(5672, config.Port);
            Assert.Equal("stock-publisher", config.Username);
            Assert.Equal("love0521", config.Password);
            Assert.Equal("/", config.VirtualHost);
        }

        [Fact]
        public void FromUri_WithEncodedVhost_ShouldDecodeCorrectly()
        {
            // Arrange
            var uri = "amqp://user:pass@localhost:5672/my-vhost";

            // Act
            var config = RabbitMqConfig.FromUri(uri);

            // Assert
            Assert.Equal("my-vhost", config.VirtualHost);
        }

        [Fact]
        public void FromUri_WithDefaultVhost_ShouldUseSlash()
        {
            // Arrange
            var uri = "amqp://user:pass@localhost:5672/%2F";

            // Act
            var config = RabbitMqConfig.FromUri(uri);

            // Assert
            Assert.Equal("/", config.VirtualHost);
        }

        [Fact]
        public void DefaultValues_ShouldBeSet()
        {
            // Arrange & Act
            var config = new RabbitMqConfig();

            // Assert
            Assert.Equal("localhost", config.Host);
            Assert.Equal(5672, config.Port);
            Assert.Equal("/", config.VirtualHost);
            Assert.Equal("guest", config.Username);
            Assert.Equal("guest", config.Password);
            Assert.Equal("stock-ex", config.ExchangeName);
            Assert.Equal("stock.twse.tick", config.RoutingKey);
            Assert.True(config.Enabled);
        }
    }
}
