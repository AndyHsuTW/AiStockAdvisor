using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Application.Services;
using AiStockAdvisor.Application.Interfaces;

namespace AiStockAdvisor.Tests.Application
{
    /// <summary>
    /// 多檔股票 Orchestrator 測試：驗證 Subscribe 與 KBarGenerator 路由。
    /// </summary>
    public class MultiStockOrchestratorTests
    {
        [Fact]
        public void Start_WithMultipleSymbols_ShouldSubscribeEach()
        {
            // Arrange
            var broker = Substitute.For<IBrokerClient>();
            var logger = Substitute.For<ILogger>();
            var orchestrator = new TradingOrchestrator(broker, logger);

            var symbols = new[] { "2327", "2330" };

            // Act
            orchestrator.Start(symbols, "user", "pass");

            // Assert — 每檔股票各被呼叫一次 Subscribe & SubscribeBest5
            broker.Received(1).Subscribe("2327");
            broker.Received(1).Subscribe("2330");
            broker.Received(1).SubscribeBest5("2327");
            broker.Received(1).SubscribeBest5("2330");
        }

        [Fact]
        public void Start_WithSingleSymbol_ShouldBeBackwardCompatible()
        {
            // Arrange
            var broker = Substitute.For<IBrokerClient>();
            var logger = Substitute.For<ILogger>();
            var orchestrator = new TradingOrchestrator(broker, logger);

            // Act
            orchestrator.Start(new[] { "2327" }, "user", "pass");

            // Assert
            broker.Received(1).Subscribe("2327");
            broker.Received(1).SubscribeBest5("2327");
        }

        [Fact]
        public void Start_WithDuplicateSymbols_ShouldSubscribeOnlyOncePerSymbol()
        {
            // Arrange
            var broker = Substitute.For<IBrokerClient>();
            var logger = Substitute.For<ILogger>();
            var orchestrator = new TradingOrchestrator(broker, logger);

            // Act
            orchestrator.Start(new[] { "2327", "2327", "2330", "2330" }, "user", "pass");

            // Assert
            broker.Received(1).Subscribe("2327");
            broker.Received(1).Subscribe("2330");
            broker.Received(1).SubscribeBest5("2327");
            broker.Received(1).SubscribeBest5("2330");
        }

        [Fact]
        public void Start_WhenTicksArriveConcurrently_ShouldNotThrow()
        {
            // Arrange
            var broker = Substitute.For<IBrokerClient>();
            var logger = Substitute.For<ILogger>();
            var orchestrator = new TradingOrchestrator(broker, logger);
            var symbols = Enumerable.Range(0, 120).Select(i => (2327 + i).ToString()).ToArray();
            var targetSymbol = symbols[0];

            Exception? backgroundException = null;
            Task? tickTask = null;
            var stopTicks = 0;
            var subscribeCount = 0;

            broker.When(b => b.Subscribe(Arg.Any<string>()))
                .Do(_ =>
                {
                    var currentCount = Interlocked.Increment(ref subscribeCount);
                    if (currentCount == 1)
                    {
                        tickTask = Task.Run(() =>
                        {
                            try
                            {
                                var t = new DateTime(2025, 1, 1, 9, 0, 0);
                                var seq = 0;
                                while (Volatile.Read(ref stopTicks) == 0)
                                {
                                    var tick = new Tick(targetSymbol, t.AddMilliseconds(seq), 100m + (seq % 3), 1);
                                    broker.OnTickReceived += Raise.Event<Action<Tick>>(tick);
                                    seq++;
                                }
                            }
                            catch (Exception ex)
                            {
                                backgroundException = ex;
                            }
                        });
                    }

                    // 放大 Start 與 HandleTick 的重疊機率
                    Thread.SpinWait(40000);
                });

            // Act
            var startException = Record.Exception(() => orchestrator.Start(symbols, "user", "pass"));
            Interlocked.Exchange(ref stopTicks, 1);
            tickTask?.Wait(TimeSpan.FromSeconds(3));

            // Assert
            Assert.Null(startException);
            Assert.Null(backgroundException);
        }

        [Fact]
        public void HandleTick_ShouldRouteToCorrectKBarGenerator()
        {
            // Arrange
            var broker = Substitute.For<IBrokerClient>();
            var logger = Substitute.For<ILogger>();
            var orchestrator = new TradingOrchestrator(broker, logger);

            // 擷取策略來觀察收到的 bar
            var receivedBars = new List<KBar>();
            var strategy = Substitute.For<ITradingStrategy>();
            strategy.Name.Returns("TestStrategy");
            strategy.When(s => s.OnBar(Arg.Any<KBar>()))
                    .Do(ci => receivedBars.Add(ci.Arg<KBar>()));
            orchestrator.RegisterStrategy(strategy);

            orchestrator.Start(new[] { "2327", "2330" }, "user", "pass");

            // Act — 模擬 broker 發出 tick
            var t0 = new DateTime(2025, 1, 1, 9, 0, 10);
            var t1 = new DateTime(2025, 1, 1, 9, 1, 10);

            // 觸發 OnTickReceived 事件
            broker.OnTickReceived += Raise.Event<Action<Tick>>(new Tick("2327", t0, 100m, 1));
            broker.OnTickReceived += Raise.Event<Action<Tick>>(new Tick("2330", t0, 500m, 1));
            broker.OnTickReceived += Raise.Event<Action<Tick>>(new Tick("2327", t1, 110m, 1));
            broker.OnTickReceived += Raise.Event<Action<Tick>>(new Tick("2330", t1, 520m, 1));

            // Assert — 兩檔股票各產生一根 bar
            Assert.Equal(2, receivedBars.Count);
            Assert.Contains(receivedBars, b => b.Symbol == "2327" && b.Open == 100m);
            Assert.Contains(receivedBars, b => b.Symbol == "2330" && b.Open == 500m);
        }

        [Fact]
        public void HandleTick_UnknownSymbol_ShouldNotThrow()
        {
            // Arrange
            var broker = Substitute.For<IBrokerClient>();
            var logger = Substitute.For<ILogger>();
            var orchestrator = new TradingOrchestrator(broker, logger);
            orchestrator.Start(new[] { "2327" }, "user", "pass");

            // Act — 送入未訂閱的 symbol，不應抛例外
            var tick = new Tick("9999", DateTime.Now, 50m, 1);
            var ex = Record.Exception(() =>
                broker.OnTickReceived += Raise.Event<Action<Tick>>(tick));

            // Assert
            Assert.Null(ex);
        }
    }
}
