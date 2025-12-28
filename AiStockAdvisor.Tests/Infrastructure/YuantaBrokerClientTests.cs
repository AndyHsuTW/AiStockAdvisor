using System;
using System.Collections.Generic;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Infrastructure.Yuanta;
using NSubstitute;
using Xunit;
using YuantaOneAPI;

namespace AiStockAdvisor.Tests.Infrastructure
{
    public class YuantaBrokerClientTests
    {
        private readonly IYuantaApiAdapter _adapter;
        private readonly YuantaBrokerClient _client;

        public YuantaBrokerClientTests()
        {
            _adapter = Substitute.For<IYuantaApiAdapter>();
            _client = new YuantaBrokerClient(_adapter);
        }

        [Fact]
        public void Login_ShouldCallOpenAndLoginOnAdapter()
        {
            // Act
            _client.Login("testUser", "testPass");

            // Assert
            _adapter.Received(1).Open(enumEnvironmentMode.PROD);
            _adapter.Received(1).Login("testUser", "testPass");
        }

        [Fact]
        public void Subscribe_ShouldCallSubscribeStockTickOnAdapter_WithCorrectSymbol()
        {
            // Arrange
            string symbol = "2330";

            // Act
            _client.Subscribe(symbol);

            // Assert
            _adapter.Received(1).SubscribeStockTick(Arg.Is<List<StockTick>>(x => 
                x.Count == 1 && 
                x[0].StockCode == "2330" && 
                x[0].MarketNo == (byte)enumMarketType.TWSE));
        }

        [Fact]
        public void Constructor_ShouldDisablePopUps()
        {
            // Assert
            _adapter.Received(1).SetPopUpMsg(false);
            _adapter.Received(1).SetLogType(enumLogType.COMMON);
        }

        [Fact]
        public void OnResponse_ShouldParseTick_WhenMarkIs2_AndIndexIsCorrect()
        {
            // Arrange
            Tick receivedTick = null;
            _client.OnTickReceived += (tick) => receivedTick = tick;

            // Build a dummy 210.10.40.10 payload (62 bytes) using big-endian numeric fields.
            // Layout (from official sample):
            // key[22], marketNo[1], stkCode[12], serialNo[4], time(h,m,s,ms)[5], buy[4], sell[4], dealPrice[4], dealVol[4], inOut[1], type[1]
            byte[] data = new byte[62];
            int offset = 0;

            // key 22 bytes
            offset += 22;

            // marketNo
            data[offset++] = (byte)enumMarketType.TWSE;

            // stock code
            var symbolBytes = System.Text.Encoding.ASCII.GetBytes("2330");
            Array.Copy(symbolBytes, 0, data, offset, symbolBytes.Length);
            offset += 12;

            // serialNo (big-endian) - set 1
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x01;

            // time: 10:30:00.000
            data[offset++] = 10; // hour
            data[offset++] = 30; // min
            data[offset++] = 0;  // sec
            data[offset++] = 0;  // ms hi
            data[offset++] = 0;  // ms lo

            // buyPrice (0)
            offset += 4;
            // sellPrice (0)
            offset += 4;

            // dealPrice = 12345 (0x00003039)
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x30;
            data[offset++] = 0x39;

            // dealVol = 10
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x0A;

            // inOut/type
            data[offset++] = 0;
            data[offset++] = 0;

            // Act
            _adapter.OnResponse += Raise.Event<OnResponseEventHandler>(2, (uint)0, "210.10.40.10", (object)null, (object)data);

            // Assert
            Assert.NotNull(receivedTick);
            Assert.Equal("2330", receivedTick.Symbol);
            Assert.Equal(123.45m, receivedTick.Price);
            Assert.Equal(10, receivedTick.Volume);
            Assert.Equal(DateTime.Today.Year, receivedTick.Time.Year);
            Assert.Equal(10, receivedTick.Time.Hour);
        }

        [Fact]
        public void OnResponse_ShouldDeduplicateSameTick_WhenPayloadRepeats()
        {
            int receivedCount = 0;
            _client.OnTickReceived += (tick) => receivedCount++;

            byte[] data = new byte[62];
            int offset = 0;
            offset += 22; // key
            data[offset++] = (byte)enumMarketType.TWSE;
            var symbolBytes = System.Text.Encoding.ASCII.GetBytes("2330");
            Array.Copy(symbolBytes, 0, data, offset, symbolBytes.Length);
            offset += 12;

            // serialNo = 99
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x63;

            // time: 10:30:00.000
            data[offset++] = 10;
            data[offset++] = 30;
            data[offset++] = 0;
            data[offset++] = 0;
            data[offset++] = 0;

            offset += 4; // buy
            offset += 4; // sell

            // dealPrice = 12345
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x30;
            data[offset++] = 0x39;

            // dealVol = 10
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x00;
            data[offset++] = 0x0A;

            data[offset++] = 0;
            data[offset++] = 0;

            _adapter.OnResponse += Raise.Event<OnResponseEventHandler>(2, (uint)0, "210.10.40.10", (object)null, (object)data);
            _adapter.OnResponse += Raise.Event<OnResponseEventHandler>(2, (uint)0, "210.10.40.10", (object)null, (object)data);

            Assert.Equal(1, receivedCount);
        }
    }
}
