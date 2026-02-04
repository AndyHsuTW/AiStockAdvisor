using System;
using System.Text.Json;
using AiStockAdvisor.Contracts.Configuration;
using AiStockAdvisor.Contracts.Constants;
using AiStockAdvisor.Contracts.Messages;
using AiStockAdvisor.Contracts.Models;
using Xunit;

namespace AiStockAdvisor.Tests.Contracts
{
    public class TickTests
    {
        [Fact]
        public void Constructor_ShouldSetExpectedValues()
        {
            var time = new DateTime(2026, 2, 3, 9, 30, 0);
            var tick = new Tick("2327", time, 500.5m, 100);

            Assert.Equal("2327", tick.Symbol);
            Assert.Equal(500.5m, tick.Price);
            Assert.Equal(100m, tick.Volume);
            Assert.Equal(time, tick.Time);
        }
    }

    public class KBarTests
    {
        [Fact]
        public void ComputedProperties_ShouldMatchExpectedValues()
        {
            var kbar = new KBar(DateTime.Now, 500m, 510m, 495m, 505m, 1000m);

            Assert.Equal(5m, kbar.Body);
            Assert.Equal(5m, kbar.UpperShadow);
            Assert.Equal(5m, kbar.LowerShadow);
            Assert.True(kbar.IsBullish);
        }
    }

    public class TickMessageTests
    {
        [Fact]
        public void ToDomainTick_ShouldConvertCorrectly()
        {
            var message = new TickMessage
            {
                TradeDate = "2026-02-03",
                MarketNo = 1,
                StockCode = "2327",
                SerialNo = 1,
                DealPriceRaw = 5005000,
                DealVolRaw = 100,
                TickTime = new TickTimeInfo
                {
                    Year = 2026,
                    Month = 2,
                    Day = 3,
                    Hour = 9,
                    Minute = 30,
                    Second = 0,
                    Millisecond = 0
                }
            };

            var domain = message.ToDomainTick();

            Assert.Equal("2327", domain.Symbol);
            Assert.Equal(500.5m, domain.Price);
            Assert.Equal(100m, domain.Volume);
            Assert.Equal(new DateTime(2026, 2, 3, 9, 30, 0), domain.Time);
        }

        [Fact]
        public void JsonSerializer_ShouldRoundTrip()
        {
            var message = new TickMessage
            {
                TradeDate = "2026-02-03",
                StockCode = "2327",
                DealPriceRaw = 5005000,
                TickTime = new TickTimeInfo { Year = 2026, Month = 2, Day = 3, Hour = 9, Minute = 30, Second = 0 }
            };

            var json = JsonSerializer.Serialize(message);
            var parsed = JsonSerializer.Deserialize<TickMessage>(json);

            Assert.NotNull(parsed);
            Assert.Equal(message.StockCode, parsed!.StockCode);
        }
    }

    public class ConfigurationTests
    {
        [Fact]
        public void RabbitMqConfig_ShouldHaveDefaultHost()
        {
            var config = new RabbitMqConfig();
            Assert.Equal("192.168.0.43", config.Host);
        }

        [Fact]
        public void TimescaleDbConfig_ShouldBuildConnectionString()
        {
            var config = new TimescaleDbConfig
            {
                Host = "192.168.0.43",
                Port = 5432,
                Database = "stock_data",
                Username = "postgres",
                Password = "secret"
            };

            Assert.Equal("Host=192.168.0.43;Port=5432;Database=stock_data;Username=postgres;Password=secret", config.ConnectionString);
        }
    }

    public class ConstantsTests
    {
        [Fact]
        public void ExchangeAndQueueNames_ShouldMatchExpectedValues()
        {
            Assert.Equal("stock.ticks", ExchangeNames.StockTicks);
            Assert.Equal("trading.events", ExchangeNames.TradingEvents);
            Assert.Equal("trading-core.ticks", QueueNames.TradingCoreTicks);
            Assert.Equal("db-writer.ticks", QueueNames.DbWriterTicks);
        }
    }
}
