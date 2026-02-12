using System;
using AiStockAdvisor.Application.Services;
using AiStockAdvisor.Domain;
using Xunit;

namespace AiStockAdvisor.Tests.Application
{
    /// <summary>
    /// 驗證 TickGapDetector 的缺號判斷行為。
    /// </summary>
    public class TickGapDetectorTests
    {
        /// <summary>
        /// 當序號跳號時應回報缺號事件。
        /// </summary>
        [Fact]
        public void TryDetectGap_WhenSerialNoJumps_ShouldReturnGapEvent()
        {
            // Arrange
            var detector = new TickGapDetector();
            var t0 = new DateTime(2026, 2, 11, 9, 0, 0);

            // Act
            var firstResult = detector.TryDetectGap(
                new Tick("2327", t0, 100m, 1m, marketNo: 1, serialNo: 10, tradeDate: t0.Date),
                out _);
            var secondResult = detector.TryDetectGap(
                new Tick("2327", t0.AddSeconds(1), 101m, 1m, marketNo: 1, serialNo: 13, tradeDate: t0.Date),
                out var gapEvent);

            // Assert
            Assert.False(firstResult);
            Assert.True(secondResult);
            Assert.NotNull(gapEvent);
            Assert.Equal("2327", gapEvent!.StockCode);
            Assert.Equal(10, gapEvent.PreviousSerialNo);
            Assert.Equal(13, gapEvent.CurrentSerialNo);
            Assert.Equal(11, gapEvent.MissingStartSerialNo);
            Assert.Equal(12, gapEvent.MissingEndSerialNo);
        }

        /// <summary>
        /// 重複序號與回補序號不應誤判為缺號。
        /// </summary>
        [Fact]
        public void TryDetectGap_WhenDuplicateOrOutOfOrder_ShouldNotReturnGapEvent()
        {
            // Arrange
            var detector = new TickGapDetector();
            var t0 = new DateTime(2026, 2, 11, 9, 0, 0);

            // Act
            detector.TryDetectGap(
                new Tick("3090", t0, 9000m, 1m, marketNo: 1, serialNo: 100, tradeDate: t0.Date),
                out _);
            var duplicateResult = detector.TryDetectGap(
                new Tick("3090", t0.AddSeconds(1), 9001m, 1m, marketNo: 1, serialNo: 100, tradeDate: t0.Date),
                out _);
            detector.TryDetectGap(
                new Tick("3090", t0.AddSeconds(2), 9002m, 1m, marketNo: 1, serialNo: 102, tradeDate: t0.Date),
                out _);
            var outOfOrderResult = detector.TryDetectGap(
                new Tick("3090", t0.AddSeconds(3), 9001m, 1m, marketNo: 1, serialNo: 101, tradeDate: t0.Date),
                out _);

            // Assert
            Assert.False(duplicateResult);
            Assert.False(outOfOrderResult);
        }

        /// <summary>
        /// 無效序號不應納入缺號判斷。
        /// </summary>
        [Fact]
        public void TryDetectGap_WhenSerialNoIsInvalid_ShouldIgnore()
        {
            // Arrange
            var detector = new TickGapDetector();
            var t0 = new DateTime(2026, 2, 11, 9, 0, 0);

            // Act
            var result = detector.TryDetectGap(
                new Tick("2327", t0, 100m, 1m, marketNo: 1, serialNo: 0, tradeDate: t0.Date),
                out var gapEvent);

            // Assert
            Assert.False(result);
            Assert.Null(gapEvent);
        }
    }
}
