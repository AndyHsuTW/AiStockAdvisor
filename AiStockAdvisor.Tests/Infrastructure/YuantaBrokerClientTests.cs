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

        private static DateTime GetTaipeiTradeDate()
        {
            try
            {
                TimeZoneInfo tz;
                try
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                }
                catch
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
                }

                var nowTaipei = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                return nowTaipei.Date;
            }
            catch
            {
                return DateTime.Now.Date;
            }
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
        public void SubscribeBest5_ShouldCallSubscribeFiveTickAOnAdapter_WithCorrectSymbol()
        {
            string symbol = "2330";

            _client.SubscribeBest5(symbol);

            _adapter.Received(1).SubscribeFiveTickA(Arg.Is<List<FiveTickA>>(x =>
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
            Assert.Equal((int)enumMarketType.TWSE, receivedTick.MarketNo);
            Assert.Equal(123.45m, receivedTick.Price);
            Assert.Equal(10, receivedTick.Volume);
            Assert.Equal(1, receivedTick.SerialNo);
            Assert.Equal(GetTaipeiTradeDate(), receivedTick.TradeDate);
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

        [Fact]
        public void OnResponse_ShouldParseBest5_WhenIndexIs50()
        {
            Best5Quote received = null;
            _client.OnBest5Received += quote => received = quote;

            byte[] data = new byte[116];
            int offset = 0;

            // key 22 bytes
            offset += 22;

            // marketNo
            data[offset++] = (byte)enumMarketType.TWSE;

            // stock code
            var symbolBytes = System.Text.Encoding.ASCII.GetBytes("2330");
            Array.Copy(symbolBytes, 0, data, offset, symbolBytes.Length);
            offset += 12;

            // index flag = 50
            data[offset++] = 50;

            int[] bidPrices = { 10000, 9900, 9800, 9700, 9600 };
            uint[] bidVols = { 10, 11, 12, 13, 14 };
            int[] askPrices = { 10100, 10200, 10300, 10400, 10500 };
            uint[] askVols = { 20, 21, 22, 23, 24 };

            void WriteInt(int value)
            {
                data[offset++] = (byte)((value >> 24) & 0xFF);
                data[offset++] = (byte)((value >> 16) & 0xFF);
                data[offset++] = (byte)((value >> 8) & 0xFF);
                data[offset++] = (byte)(value & 0xFF);
            }

            void WriteUInt(uint value)
            {
                data[offset++] = (byte)((value >> 24) & 0xFF);
                data[offset++] = (byte)((value >> 16) & 0xFF);
                data[offset++] = (byte)((value >> 8) & 0xFF);
                data[offset++] = (byte)(value & 0xFF);
            }

            for (int i = 0; i < 5; i++)
            {
                WriteInt(bidPrices[i]);
            }

            for (int i = 0; i < 5; i++)
            {
                WriteUInt(bidVols[i]);
            }

            for (int i = 0; i < 5; i++)
            {
                WriteInt(askPrices[i]);
            }

            for (int i = 0; i < 5; i++)
            {
                WriteUInt(askVols[i]);
            }

            _adapter.OnResponse += Raise.Event<OnResponseEventHandler>(2, (uint)0, "210.10.60.10", (object)null, (object)data);

            Assert.NotNull(received);
            Assert.Equal("2330", received.Symbol);
            Assert.Equal((int)enumMarketType.TWSE, received.MarketNo);
            Assert.Equal(100.00m, received.Bids[0].Price);
            Assert.Equal(10, received.Bids[0].Volume);
            Assert.Equal(101.00m, received.Asks[0].Price);
            Assert.Equal(20, received.Asks[0].Volume);
        }
    }
}
