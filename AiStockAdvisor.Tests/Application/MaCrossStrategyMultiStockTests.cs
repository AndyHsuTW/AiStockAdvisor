using System;
using System.Collections.Generic;
using Xunit;
using NSubstitute;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Application.Services;
using AiStockAdvisor.Application.Interfaces;

namespace AiStockAdvisor.Tests.Application
{
    /// <summary>
    /// MaCrossStrategy 多股隔離測試：驗證不同 symbol 的 MA 各自獨立計算。
    /// </summary>
    public class MaCrossStrategyMultiStockTests
    {
        [Fact]
        public void OnBar_DifferentSymbols_ShouldCalculateMAIndependently()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var strategy = new MaCrossStrategy(logger, shortPeriod: 2, longPeriod: 3);

            var time = new DateTime(2025, 1, 1, 9, 1, 0);

            // Act — 分別餵入兩檔股票的 KBar
            // Symbol A: 收盤價 100, 102, 104 → shortMA(2)=103, longMA(3)=102
            strategy.OnBar(new KBar("2327", time, 100, 100, 100, 100, 1));
            strategy.OnBar(new KBar("2327", time.AddMinutes(1), 102, 102, 102, 102, 1));
            strategy.OnBar(new KBar("2327", time.AddMinutes(2), 104, 104, 104, 104, 1));

            // Symbol B: 收盤價 500, 490, 480 → shortMA(2)=485, longMA(3)=490
            strategy.OnBar(new KBar("2330", time, 500, 500, 500, 500, 1));
            strategy.OnBar(new KBar("2330", time.AddMinutes(1), 490, 490, 490, 490, 1));
            strategy.OnBar(new KBar("2330", time.AddMinutes(2), 480, 480, 480, 480, 1));

            // Assert — 日誌中有正確的 symbol 標記
            // Symbol A 是 BULLISH (shortMA 103 > longMA 102)
            logger.Received().LogInformation(
                Arg.Is<string>(s => s.Contains("[2327]") && s.Contains("BULLISH")));

            // Symbol B 是 BEARISH (shortMA 485 < longMA 490)
            logger.Received().LogInformation(
                Arg.Is<string>(s => s.Contains("[2330]") && s.Contains("BEARISH")));
        }

        [Fact]
        public void OnBar_SingleSymbol_ShouldWorkAsBeforeMultiStock()
        {
            // Arrange
            var logger = Substitute.For<ILogger>();
            var strategy = new MaCrossStrategy(logger, shortPeriod: 2, longPeriod: 3);

            var time = new DateTime(2025, 1, 1, 9, 1, 0);

            // Act — 只餵入單一股票
            strategy.OnBar(new KBar("2327", time, 100, 100, 100, 100, 1));
            strategy.OnBar(new KBar("2327", time.AddMinutes(1), 102, 102, 102, 102, 1));
            strategy.OnBar(new KBar("2327", time.AddMinutes(2), 104, 104, 104, 104, 1));

            // Assert — 應正常產生 MA 日誌
            logger.Received().LogInformation(
                Arg.Is<string>(s => s.Contains("MA2") && s.Contains("MA3")));
        }
    }
}
