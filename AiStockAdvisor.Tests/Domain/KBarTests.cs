using System;
using Xunit;
using AiStockAdvisor.Domain;

namespace AiStockAdvisor.Tests.Domain
{
    public class KBarTests
    {
        [Fact]
        public void Constructor_ShouldThrowArgumentException_WhenHighIsLessThanLow()
        {
            // Arrange
            var time = DateTime.Now;
            decimal open = 100;
            decimal high = 90; // Invalid: High < Low
            decimal low = 95;
            decimal close = 95;
            decimal volume = 1000;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new KBar("2327", time, open, high, low, close, volume));
        }

        [Fact]
        public void Constructor_ShouldCreateKBar_WhenDataIsValid()
        {
             // Arrange
            var time = DateTime.Now;
            decimal open = 100;
            decimal high = 105;
            decimal low = 95;
            decimal close = 102;
            decimal volume = 1000;

            // Act
            var kbar = new KBar("2327", time, open, high, low, close, volume);

            // Assert
            Assert.Equal("2327", kbar.Symbol);
            Assert.Equal(high, kbar.High);
            Assert.Equal(low, kbar.Low);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenSymbolIsNull()
        {
            // Arrange
            var time = DateTime.Now;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new KBar(null!, time, 100, 105, 95, 102, 1000));
        }
    }
}
