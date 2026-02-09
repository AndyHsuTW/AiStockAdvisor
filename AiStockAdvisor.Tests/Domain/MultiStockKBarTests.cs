using System;
using System.Collections.Generic;
using Xunit;
using AiStockAdvisor.Domain;

namespace AiStockAdvisor.Tests.Domain
{
    /// <summary>
    /// 多檔股票 KBar 聚合測試：驗證不同 symbol 的 Tick 各自獨立產生 KBar。
    /// </summary>
    public class MultiStockKBarTests
    {
        private static Tick MakeTick(string symbol, DateTime time, decimal price, decimal volume = 1)
            => new Tick(symbol, time, price, volume);

        [Fact]
        public void DifferentSymbol_Ticks_ShouldProduceSeparateBars()
        {
            // Arrange
            var barsA = new List<KBar>();
            var barsB = new List<KBar>();

            var genA = new KBarGenerator("2327", TimeSpan.FromMinutes(1));
            var genB = new KBarGenerator("2330", TimeSpan.FromMinutes(1));
            genA.OnBarClosed += bar => barsA.Add(bar);
            genB.OnBarClosed += bar => barsB.Add(bar);

            var t0 = new DateTime(2025, 1, 1, 9, 0, 10);
            var t1 = new DateTime(2025, 1, 1, 9, 1, 10); // 觸發前一根 bar close

            // Act — 送入 symbol A 的 tick
            genA.Update(MakeTick("2327", t0, 100m));
            genA.Update(MakeTick("2327", t1, 110m));

            // 送入 symbol B 的 tick
            genB.Update(MakeTick("2330", t0, 500m));
            genB.Update(MakeTick("2330", t1, 520m));

            // Assert
            Assert.Single(barsA);
            Assert.Equal("2327", barsA[0].Symbol);
            Assert.Equal(100m, barsA[0].Open);

            Assert.Single(barsB);
            Assert.Equal("2330", barsB[0].Symbol);
            Assert.Equal(500m, barsB[0].Open);
        }

        [Fact]
        public void KBarGenerator_ShouldThrow_WhenSymbolIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new KBarGenerator(null!, TimeSpan.FromMinutes(1)));
        }

        [Fact]
        public void KBar_Symbol_ShouldMatchGenerator()
        {
            // Arrange
            var bars = new List<KBar>();
            var gen = new KBarGenerator("2454", TimeSpan.FromMinutes(1));
            gen.OnBarClosed += bar => bars.Add(bar);

            var t0 = new DateTime(2025, 1, 1, 9, 0, 5);
            var t1 = new DateTime(2025, 1, 1, 9, 1, 5);

            // Act
            gen.Update(MakeTick("2454", t0, 800m));
            gen.Update(MakeTick("2454", t1, 810m));

            // Assert
            Assert.Single(bars);
            Assert.Equal("2454", bars[0].Symbol);
        }

        [Fact]
        public void InterleavedTicks_ShouldNotCrossContaminate()
        {
            // Arrange — 模擬兩檔股票交互送 tick 到各自的 generator
            var barsA = new List<KBar>();
            var barsB = new List<KBar>();

            var genA = new KBarGenerator("2327", TimeSpan.FromMinutes(1));
            var genB = new KBarGenerator("2330", TimeSpan.FromMinutes(1));
            genA.OnBarClosed += bar => barsA.Add(bar);
            genB.OnBarClosed += bar => barsB.Add(bar);

            var t0 = new DateTime(2025, 1, 1, 9, 0, 5);
            var t0b = new DateTime(2025, 1, 1, 9, 0, 30);
            var t1 = new DateTime(2025, 1, 1, 9, 1, 5);

            // Act — 交互送入
            genA.Update(MakeTick("2327", t0, 100m));
            genB.Update(MakeTick("2330", t0, 500m));
            genA.Update(MakeTick("2327", t0b, 105m)); // 同一根 bar 內更新 high
            genB.Update(MakeTick("2330", t0b, 490m)); // 同一根 bar 內更新 low
            genA.Update(MakeTick("2327", t1, 102m));   // close bar
            genB.Update(MakeTick("2330", t1, 510m));   // close bar

            // Assert
            Assert.Single(barsA);
            Assert.Equal(105m, barsA[0].High);
            Assert.Equal(100m, barsA[0].Low);

            Assert.Single(barsB);
            Assert.Equal(500m, barsB[0].High);
            Assert.Equal(490m, barsB[0].Low);
        }
    }
}
